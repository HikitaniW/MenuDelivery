
import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  Dish,
  DishFilters,
  DishService,
  CreateDishRequest,
  UpdateDishRequest
} from './services/dish.service';
import { AuthService } from './services/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App implements OnInit {
  protected dishes = signal<Dish[]>([]);
  protected loading = signal<boolean>(true);
  protected error = signal<string>('');

  protected selectedCategory = '';
  protected selectedAvailability = '';
  protected maxPrice = '';
  protected selectedSort = '';

  protected showAuthForm = signal(false);
  protected authMode = signal<'login' | 'register'>('login');
  protected authEmail = '';
  protected authPassword = '';
  protected authMessage = '';
  protected authBusy = signal(false);

  protected currentEmail = signal(
    localStorage.getItem('menu_delivery_email') || ''
  );

  protected newDishName = '';
  protected newDishPrice = '';
  protected newDishWeight = '';
  protected newDishCalories = '';
  protected newDishCategory = '1';
  protected newDishAvailable = true;
  protected selectedImage: File | null = null;

  protected addDishBusy = signal(false);
  protected addDishMessage = signal('');
  protected addDishMessageType = signal<'success' | 'error'>('success');

  protected editingDishId = signal<number | null>(null);
  protected editDishName = '';
  protected editDishPrice = '';
  protected editDishWeight = '';
  protected editDishCalories = '';
  protected editDishCategory = '1';
  protected editDishAvailable = true;
  protected editDishImage: File | null = null;
  protected editDishBusy = signal(false);
  protected editDishMessage = '';

  constructor(
    private dishService: DishService,
    private authService: AuthService
  ) {}

  ngOnInit(): void {
    this.loadDishes();
  }

  protected loadDishes(): void {
    this.loading.set(true);
    this.error.set('');

    const filters: DishFilters = {};

    if (this.selectedCategory !== '') {
      filters.categoryId = Number(this.selectedCategory);
    }

    if (this.selectedAvailability === 'true') {
      filters.isAvailable = true;
    } else if (this.selectedAvailability === 'false') {
      filters.isAvailable = false;
    }

    if (this.maxPrice !== '') {
      const price = Number(this.maxPrice);
      if (Number.isFinite(price) && price >= 0) {
        filters.maxPrice = price;
      }
    }

    if (this.selectedSort === 'price' || this.selectedSort === 'calories') {
      filters.sortBy = this.selectedSort;
    }

    this.dishService.getDishes(1, 50, filters).subscribe({
      next: (response) => {
        this.dishes.set(response.items);
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Ошибка загрузки блюд:', err);
        this.error.set('Не удалось загрузить меню. Проверьте сервер.');
        this.loading.set(false);
      }
    });
  }

  protected resetFilters(): void {
    this.selectedCategory = '';
    this.selectedAvailability = '';
    this.maxPrice = '';
    this.selectedSort = '';
    this.loadDishes();
  }

  protected openAuth(mode: 'login' | 'register' = 'login'): void {
    this.authMode.set(mode);
    this.authMessage = '';
    this.authEmail = '';
    this.authPassword = '';
    this.showAuthForm.set(true);
  }

  protected closeAuth(): void {
    this.showAuthForm.set(false);
    this.authMessage = '';
  }

  protected submitAuth(): void {
    this.authMessage = '';

    if (!this.authEmail.trim() || !this.authPassword) {
      this.authMessage = 'Заполните email и пароль.';
      return;
    }

    this.authBusy.set(true);

    if (this.authMode() === 'register') {
      this.authService.register({
        email: this.authEmail.trim(),
        password: this.authPassword
      }).subscribe({
        next: () => {
          this.authBusy.set(false);
          this.authMode.set('login');
          this.authPassword = '';
          this.authMessage = 'Регистрация успешна! Теперь войдите.';
        },
        error: (err) => {
          this.authBusy.set(false);
          this.authMessage = err.status === 409
            ? 'Пользователь с таким email уже существует.'
            : err.error || 'Не удалось зарегистрироваться.';
        }
      });
      return;
    }

    this.authService.login(
      this.authEmail.trim(),
      this.authPassword
    ).subscribe({
      next: (response) => {
        this.currentEmail.set(response.email);
        this.authBusy.set(false);
        this.showAuthForm.set(false);
        this.authPassword = '';
        this.authMessage = '';
      },
      error: (err) => {
        this.authBusy.set(false);
        this.authMessage = err.status === 401
          ? 'Неверный email или пароль.'
          : 'Ошибка входа. Проверьте соединение с сервером.';
      }
    });
  }

  protected logout(): void {
    this.authService.logout();
    this.currentEmail.set('');
    this.closeAuth();
    this.addDishMessage.set('');
    this.cancelEdit();
  }

  protected onImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;

    this.addDishMessage.set('');

    if (!file) {
      this.selectedImage = null;
      return;
    }

    if (!this.isValidImage(file)) {
      this.selectedImage = null;
      input.value = '';
      this.showAddDishError('Выберите JPG, PNG или WEBP размером до 5 МБ.');
      return;
    }

    this.selectedImage = file;
  }

  protected submitNewDish(): void {
    this.addDishMessage.set('');

    if (!this.currentEmail()) {
      this.showAddDishError('Сначала войдите в аккаунт.');
      return;
    }

    const name = this.newDishName.trim();
    const price = Number(this.newDishPrice);
    const weightGrams = Number(this.newDishWeight);
    const calories = Number(this.newDishCalories);
    const categoryId = Number(this.newDishCategory);

    if (
      !name ||
      !Number.isFinite(price) ||
      !Number.isFinite(weightGrams) ||
      !Number.isFinite(calories) ||
      price <= 0 ||
      weightGrams <= 0 ||
      calories < 0 ||
      !Number.isInteger(categoryId) ||
      categoryId <= 0
    ) {
      this.showAddDishError('Проверьте правильность заполнения всех полей.');
      return;
    }

    this.addDishBusy.set(true);

    if (this.selectedImage) {
      this.dishService.uploadImage(this.selectedImage).subscribe({
        next: (uploadResponse) => {
          this.createDishOnServer(
            name, price, weightGrams, calories,
            categoryId, uploadResponse.fileName
          );
        },
        error: (err) => {
          console.error('Ошибка загрузки фотографии:', err);
          this.addDishBusy.set(false);
          this.showAddDishError(
            err.status === 401
              ? 'Сессия истекла. Войдите в аккаунт снова.'
              : 'Не удалось загрузить фотографию.'
          );
        }
      });
      return;
    }

    this.createDishOnServer(
      name, price, weightGrams, calories, categoryId, null
    );
  }

  private createDishOnServer(
    name: string,
    price: number,
    weightGrams: number,
    calories: number,
    categoryId: number,
    imageFileName: string | null
  ): void {
    const dish: CreateDishRequest = {
      name,
      price,
      weightGrams,
      calories,
      isAvailable: this.newDishAvailable,
      categoryId,
      imageFileName
    };

    this.dishService.createDish(dish).subscribe({
      next: () => {
        this.addDishBusy.set(false);
        this.addDishMessageType.set('success');
        this.addDishMessage.set('Блюдо успешно добавлено!');
        this.clearNewDishForm();
        this.loadDishes();
      },
      error: (err) => {
        console.error('Ошибка создания блюда:', err);
        this.addDishBusy.set(false);

        if (err.status === 409) {
          this.showAddDishError('У вас уже есть блюдо с таким названием.');
        } else if (err.status === 401) {
          this.showAddDishError('Сессия истекла. Войдите в аккаунт снова.');
        } else if (err.status === 400) {
          this.showAddDishError('Проверьте введённые данные и категорию.');
        } else {
          this.showAddDishError('Не удалось добавить блюдо. Попробуйте ещё раз.');
        }
      }
    });
  }

  private clearNewDishForm(): void {
    this.newDishName = '';
    this.newDishPrice = '';
    this.newDishWeight = '';
    this.newDishCalories = '';
    this.newDishCategory = '1';
    this.newDishAvailable = true;
    this.selectedImage = null;

    const imageInput = document.getElementById('newDishImage') as HTMLInputElement | null;
    if (imageInput) {
      imageInput.value = '';
    }
  }

  private showAddDishError(message: string): void {
    this.addDishMessageType.set('error');
    this.addDishMessage.set(message);
  }

  protected isDishOwner(dish: Dish): boolean {
    const userId = Number(localStorage.getItem('menu_delivery_user_id'));
    return Number.isInteger(userId) && userId > 0 && dish.authorId === userId;
  }

  protected startEdit(dish: Dish): void {
    if (!this.isDishOwner(dish)) return;

    this.editingDishId.set(dish.id);
    this.editDishName = dish.name;
    this.editDishPrice = String(dish.price);
    this.editDishWeight = String(dish.weightGrams);
    this.editDishCalories = String(dish.calories);
    this.editDishCategory = String(dish.categoryId);
    this.editDishAvailable = dish.isAvailable;
    this.editDishImage = null;
    this.editDishMessage = '';
  }

  protected cancelEdit(): void {
    this.editingDishId.set(null);
    this.editDishImage = null;
    this.editDishMessage = '';
    this.editDishBusy.set(false);
  }

  protected onEditImageSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.editDishMessage = '';

    if (!file) {
      this.editDishImage = null;
      return;
    }

    if (!this.isValidImage(file)) {
      this.editDishImage = null;
      input.value = '';
      this.editDishMessage = 'Выберите JPG, PNG или WEBP размером до 5 МБ.';
      return;
    }

    this.editDishImage = file;
  }

  protected submitEdit(dish: Dish): void {
    if (!this.isDishOwner(dish) || this.editingDishId() !== dish.id) return;

    const name = this.editDishName.trim();
    const price = Number(this.editDishPrice);
    const weightGrams = Number(this.editDishWeight);
    const calories = Number(this.editDishCalories);
    const categoryId = Number(this.editDishCategory);

    if (
      !name ||
      !Number.isFinite(price) ||
      !Number.isFinite(weightGrams) ||
      !Number.isFinite(calories) ||
      price <= 0 ||
      weightGrams <= 0 ||
      calories < 0 ||
      !Number.isInteger(categoryId) ||
      categoryId <= 0
    ) {
      this.editDishMessage = 'Проверьте правильность заполнения полей.';
      return;
    }

    this.editDishBusy.set(true);
    this.editDishMessage = '';

    if (this.editDishImage) {
      this.dishService.uploadImage(this.editDishImage).subscribe({
        next: (response) => {
          this.updateDishOnServer(
            dish, name, price, weightGrams,
            calories, categoryId, response.fileName
          );
        },
        error: (err) => {
          console.error('Ошибка загрузки фото:', err);
          this.editDishBusy.set(false);
          this.editDishMessage = err.status === 401
            ? 'Сессия истекла. Войдите снова.'
            : 'Не удалось загрузить фотографию.';
        }
      });
      return;
    }

    this.updateDishOnServer(
      dish, name, price, weightGrams,
      calories, categoryId, dish.imageFileName
    );
  }

  private updateDishOnServer(
    dish: Dish,
    name: string,
    price: number,
    weightGrams: number,
    calories: number,
    categoryId: number,
    imageFileName: string | null
  ): void {
    const update: UpdateDishRequest = {
      name,
      price,
      weightGrams,
      calories,
      isAvailable: this.editDishAvailable,
      categoryId,
      imageFileName
    };

    this.dishService.updateDish(dish.id, update).subscribe({
      next: () => {
        this.editDishBusy.set(false);
        this.cancelEdit();
        this.loadDishes();
      },
      error: (err) => {
        console.error('Ошибка изменения блюда:', err);
        this.editDishBusy.set(false);

        if (err.status === 409) {
          this.editDishMessage = 'У вас уже есть блюдо с таким названием.';
        } else if (err.status === 401) {
          this.editDishMessage = 'Сессия истекла. Войдите снова.';
        } else if (err.status === 403) {
          this.editDishMessage = 'Вы не можете редактировать это блюдо.';
        } else if (err.status === 400) {
          this.editDishMessage = 'Проверьте введённые данные.';
        } else {
          this.editDishMessage = 'Не удалось сохранить изменения.';
        }
      }
    });
  }

  protected deleteDish(dish: Dish): void {
    if (!this.isDishOwner(dish)) return;

    const confirmed = window.confirm(
      `Удалить блюдо «${dish.name}»? Это действие нельзя отменить.`
    );

    if (!confirmed) return;

    this.dishService.deleteDish(dish.id).subscribe({
      next: () => {
        if (this.editingDishId() === dish.id) {
          this.cancelEdit();
        }
        this.loadDishes();
      },
      error: (err) => {
        console.error('Ошибка удаления блюда:', err);
        if (err.status === 401) {
          window.alert('Сессия истекла. Войдите снова.');
        } else if (err.status === 403) {
          window.alert('Вы не можете удалить это блюдо.');
        } else {
          window.alert('Не удалось удалить блюдо.');
        }
      }
    });
  }

  private isValidImage(file: File): boolean {
    return [
      'image/jpeg',
      'image/png',
      'image/webp'
    ].includes(file.type) && file.size <= 5 * 1024 * 1024;
  }

  protected formatPrice(price: number): string {
    return new Intl.NumberFormat('ru-RU').format(price) + ' сум';
  }

  protected getDishEmoji(dish: Dish): string {
    if (dish.categoryId === 1) return '🍕';
    if (dish.categoryId === 2) return '🍔';
    if (dish.categoryId === 3) return '🥤';
    if (dish.categoryId === 4) return '🍰';
    return '🍽️';
  }
}
