from abc import ABC, abstractmethod
from datetime import date, datetime
from typing import Optional


class Discountable(ABC):
    @abstractmethod
    def apply_discount(self, percentage: float) -> float:
        pass


class PizzaBase(ABC):
    VALID_SIZES = {"mała": 0.8, "średnia": 1.0, "duża": 1.3, "XXL": 1.6}

    def __init__(self, name: str, base_price: float, size: str = "średnia"):
        self._name = name
        self._size = self._validate_size(size)
        self._base_price = self._validate_price(base_price)
        self._toppings: list["Topping"] = []

    @staticmethod
    def _validate_price(price: float) -> float:
        if price < 0:
            raise ValueError(f"Cena nie może być ujemna: {price}")
        return price

    def _validate_size(self, size: str) -> str:
        if size not in self.VALID_SIZES:
            raise ValueError(
                f"Nieprawidłowy rozmiar '{size}'. Dostępne: {list(self.VALID_SIZES.keys())}"
            )
        return size

    @property
    def name(self) -> str:
        return self._name

    @property
    def size(self) -> str:
        return self._size

    @size.setter
    def size(self, value: str):
        self._size = self._validate_size(value)

    @property
    def base_price(self) -> float:
        return self._base_price

    @base_price.setter
    def base_price(self, value: float):
        self._base_price = self._validate_price(value)

    def add_topping(self, topping: "Topping") -> None:
        self._toppings.append(topping)

    def remove_topping(self, name: str) -> bool:
        for t in self._toppings:
            if t.name.lower() == name.lower():
                self._toppings.remove(t)
                return True
        return False

    def get_price(self) -> float:
        size_multiplier = self.VALID_SIZES[self._size]
        toppings_cost = sum(t.extra_cost for t in self._toppings)
        return round((self._base_price + toppings_cost) * size_multiplier, 2)

    def list_toppings(self) -> str:
        if not self._toppings:
            return "bez dodatków"
        return ", ".join(t.name for t in self._toppings)

    @abstractmethod
    def pizza_type(self) -> str:
        pass

    @abstractmethod
    def get_description(self) -> str:
        pass

    def __str__(self) -> str:
        return (
            f"[{self.pizza_type()}] {self._name} ({self._size}) | "
            f"{self.list_toppings()} | {self.get_price():.2f} zł"
        )

    def __repr__(self) -> str:
        return f"{self.__class__.__name__}(name={self._name!r}, size={self._size!r})"


class Topping:
    def __init__(self, name: str, extra_cost: float, is_vegan: bool = False):
        self._name = name
        self._extra_cost = PizzaBase._validate_price(extra_cost)
        self._is_vegan = is_vegan

    @property
    def name(self) -> str:
        return self._name

    @property
    def extra_cost(self) -> float:
        return self._extra_cost

    @property
    def is_vegan(self) -> bool:
        return self._is_vegan

    def __str__(self) -> str:
        vegan_tag = "Tak" if self._is_vegan else "Nie"
        return f"{self._name}{vegan_tag} (+{self._extra_cost:.2f} zł)"


class ClassicPizza(PizzaBase):
    def __init__(
        self,
        name: str,
        base_price: float,
        size: str = "średnia",
        sauce: str = "pomidorowy",
    ):
        super().__init__(name, base_price, size)
        self._sauce = sauce

    @property
    def sauce(self) -> str:
        return self._sauce

    @sauce.setter
    def sauce(self, value: str):
        if not value.strip():
            raise ValueError("Nazwa sosu nie może być pusta.")
        self._sauce = value

    def pizza_type(self) -> str:
        return "Klasyczna"

    def get_description(self) -> str:
        return (
            f"Klasyczna pizza '{self._name}' na sosie {self._sauce}. "
            f"Rozmiar: {self._size}. Dodatki: {self.list_toppings()}. "
            f"Cena: {self.get_price():.2f} zł."
        )


class VeganPizza(PizzaBase):
    def __init__(self, name: str, base_price: float, size: str = "średnia"):
        super().__init__(name, base_price, size)

    def add_topping(self, topping: Topping) -> None:
        if not topping.is_vegan:
            raise ValueError(
                f"Dodatek '{topping.name}' nie jest wegański! "
                f"VeganPizza akceptuje tylko wegańskie składniki."
            )
        super().add_topping(topping)

    def pizza_type(self) -> str:
        return "Wegańska"

    def get_description(self) -> str:
        return (
            f"Wegańska pizza '{self._name}' – 100% roślinne składniki."
            f"Rozmiar: {self._size}. Dodatki: {self.list_toppings()}. "
            f"Cena: {self.get_price():.2f} zł."
        )


class PremiumPizza(PizzaBase):
    def __init__(
        self, name: str, base_price: float, size: str = "duża", chef_note: str = ""
    ):
        super().__init__(name, base_price, size)
        self._chef_note = chef_note

    @property
    def chef_note(self) -> str:
        return self._chef_note

    @chef_note.setter
    def chef_note(self, value: str):
        self._chef_note = value

    def pizza_type(self) -> str:
        return "Premium"

    def get_description(self) -> str:
        note = f'Notatka szefa: "{self._chef_note}".' if self._chef_note else ""
        return (
            f"Pizza premium '{self._name}'. "
            f"Rozmiar: {self._size}. Dodatki: {self.list_toppings()}. "
            f"Cena: {self.get_price():.2f} zł.{note}"
        )


class SeasonalPizza(PremiumPizza, Discountable):
    def __init__(
        self,
        name: str,
        base_price: float,
        size: str = "duża",
        valid_until: Optional[date] = None,
        chef_note: str = "",
    ):
        PremiumPizza.__init__(self, name, base_price, size, chef_note)
        self._valid_until = valid_until
        self._discount: float = 0.0

    @property
    def valid_until(self) -> Optional[date]:
        return self._valid_until

    @property
    def discount(self) -> float:
        return self._discount

    def is_offer_active(self) -> bool:
        if self._valid_until is None:
            return True
        return date.today() <= self._valid_until

    def apply_discount(self, percentage: float) -> float:
        if not (0 <= percentage <= 100):
            raise ValueError("Procent rabatu musi być między 0 a 100.")
        self._discount = percentage
        discounted = self.get_price() * (1 - percentage / 100)
        return round(discounted, 2)

    def pizza_type(self) -> str:
        active = "Dostępna" if self.is_offer_active() else "WYGASŁA"
        return f"Sezonowa {active}"

    def get_description(self) -> str:
        base = super().get_description()
        if self._valid_until:
            base += f" Oferta ważna do: {self._valid_until.strftime('%d.%m.%Y')}."
        if self._discount > 0:
            base += f" Rabat: {self._discount:.0f}% → {self.apply_discount(self._discount):.2f} zł."
        return base


class Customer:
    def __init__(
        self,
        first_name: str,
        last_name: str,
        email: str,
        phone: str = "",
        age: int = 18,
    ):
        self._first_name = first_name
        self._last_name = last_name
        self.email = email
        self.phone = phone
        self.age = age
        self.__order_history: list[int] = []

    @property
    def first_name(self) -> str:
        return self._first_name

    @first_name.setter
    def first_name(self, value: str):
        value = value.strip()
        if not value:
            raise ValueError("Imię nie może być puste.")
        self._first_name = value

    @property
    def last_name(self) -> str:
        return self._last_name

    @last_name.setter
    def last_name(self, value: str):
        if not value.strip():
            raise ValueError("Nazwisko nie może być puste.")
        self._last_name = value.strip()

    @property
    def full_name(self) -> str:
        return f"{self._first_name} {self._last_name}"

    @property
    def email(self) -> str:
        return self._email

    @email.setter
    def email(self, value: str):
        if "@" not in value or "." not in value.split("@")[-1]:
            raise ValueError(f"Nieprawidłowy adres e-mail: '{value}'")
        self._email = value.lower().strip()

    @property
    def phone(self) -> str:
        return self._phone

    @phone.setter
    def phone(self, value: str):
        digits = value.replace(" ", "").replace("-", "").replace("+", "")
        if value and not digits.isdigit():
            raise ValueError(f"Telefon zawiera niedozwolone znaki: '{value}'")
        self._phone = value

    @property
    def age(self) -> int:
        return self._age

    @age.setter
    def age(self, value: int):
        if not (0 < value < 130):
            raise ValueError(f"Nieprawidłowy wiek: {value}")
        self._age = value

    def add_order_to_history(self, order_id: int) -> None:
        self.__order_history.append(order_id)

    def get_order_history(self) -> list[int]:
        return list(self.__order_history)

    def orders_count(self) -> int:
        return len(self.__order_history)

    def __str__(self) -> str:
        return (
            f"Klient: {self.full_name} | "
            f"Email: {self._email} | "
            f"Zamówień: {self.orders_count()}"
        )


class Order:
    _id_counter: int = 1000

    def __init__(self, customer: Customer, delivery: bool = True, notes: str = ""):
        self._id = Order._id_counter
        Order._id_counter += 1
        self._customer = customer
        self._pizzas: list[PizzaBase] = []
        self._delivery = delivery
        self._notes = notes
        self._created_at = datetime.now()
        self._is_finalized = False

    @property
    def order_id(self) -> int:
        return self._id

    @property
    def customer(self) -> Customer:
        return self._customer

    @property
    def is_delivery(self) -> bool:
        return self._delivery

    @property
    def is_finalized(self) -> bool:
        return self._is_finalized

    def add_item(self, pizza: PizzaBase) -> None:
        if self._is_finalized:
            raise RuntimeError("Nie można modyfikować sfinalizowanego zamówienia.")
        self._pizzas.append(pizza)

    def remove_item(self, index: int) -> bool:
        if self._is_finalized:
            raise RuntimeError("Nie można modyfikować sfinalizowanego zamówienia.")
        if 0 <= index < len(self._pizzas):
            self._pizzas.pop(index)
            return True
        return False

    def item_count(self) -> int:
        return len(self._pizzas)

    def calculate_total(self) -> float:
        delivery_fee = 5.0 if self._delivery else 0.0
        return round(sum(p.get_price() for p in self._pizzas) + delivery_fee, 2)

    def finalize(self) -> None:
        if not self._pizzas:
            raise ValueError("Nie można złożyć pustego zamówienia.")
        self._is_finalized = True
        self._customer.add_order_to_history(self._id)

    def print_summary(self) -> None:
        sep = "=" * 20
        print(f"\n{sep}")
        print(f"ZAMÓWIENIE #{self._id}")
        print(f"Klient: {self._customer.full_name}")
        print(f"E-mail: {self._customer.email}")
        print(f"Data: {self._created_at.strftime('%d.%m.%Y %H:%M')}")
        print(
            f"Dostawa  : {'TAK (+5.00 zł)' if self._delivery else 'NIE (odbiór osobisty)'}"
        )
        if self._notes:
            print(f"  Uwagi    : {self._notes}")
        print(f"Status: {'Złożone' if self._is_finalized else ' W trakcie'}")
        print(f"{'-' * 20}")
        if not self._pizzas:
            print("(brak pozycji)")
        for i, pizza in enumerate(self._pizzas, 1):
            print(f"  {i}. {pizza}")
        print(f"{'-' * 20}")
        subtotal = sum(p.get_price() for p in self._pizzas)
        delivery_fee = 5.0 if self._delivery else 0.0
        print(f"Suma za pizze: {subtotal:.2f} zł")
        if self._delivery:
            print(f"Dostawa: {delivery_fee:.2f} zł")
        print(f"RAZEM: {self.calculate_total():.2f} zł")
        print(f"{sep}\n")


class OrderManager:
    _all_orders: list[Order] = []
    _pizza_catalog: dict[str, PizzaBase] = {}

    @classmethod
    def register_order(cls, order: Order) -> None:
        if not order.is_finalized:
            raise ValueError("Można rejestrować tylko sfinalizowane zamówienia.")
        cls._all_orders.append(order)

    @classmethod
    def place_order(
        cls,
        customer: Customer,
        pizzas: list[PizzaBase],
        delivery: bool = True,
        notes: str = "",
    ) -> Order:
        order = Order(customer, delivery, notes)
        for pizza in pizzas:
            order.add_item(pizza)
        order.finalize()
        cls.register_order(order)
        return order

    @classmethod
    def add_to_catalog(cls, pizza: PizzaBase) -> None:
        cls._pizza_catalog[pizza.name.lower()] = pizza

    @classmethod
    def get_from_catalog(cls, name: str) -> Optional[PizzaBase]:
        return cls._pizza_catalog.get(name.lower())

    @staticmethod
    def count_by_type(pizza_type: str) -> int:
        count = 0
        for order in OrderManager._all_orders:
            for pizza in order._pizzas:
                if pizza_type.lower() in pizza.pizza_type().lower():
                    count += 1
        return count

    @classmethod
    def print_report(cls) -> None:
        sep = "=" * 20
        total_orders = len(cls._all_orders)
        total_revenue = sum(o.calculate_total() for o in cls._all_orders)
        print(f"\n{sep}")
        print("RAPORT ZAMÓWIEŃ")
        print(f"{'-' * 20}")
        print(f"Łączna liczba zamówień: {total_orders}")
        print(f"Łączny przychód: {total_revenue:.2f} zł")
        print(f"{'-' * 20}")
        types = ["Klasyczna", "Wegańska", "Premium", "Sezonowa"]
        for t in types:
            count = cls.count_by_type(t)
            if count:
                print(f"Pizze '{t}': {count} szt.")
        print(f"{sep}\n")


class ToppingCatalog:
    _toppings: dict[str, Topping] = {}

    @classmethod
    def register(cls, topping: Topping) -> None:
        cls._toppings[topping.name.lower()] = topping

    @classmethod
    def get(cls, name: str) -> Topping:
        key = name.lower()
        if key not in cls._toppings:
            raise KeyError(f"Nie znaleziono dodatku '{name}' w katalogu.")
        return cls._toppings[key]

    @classmethod
    def list_all(cls) -> None:
        print("\n── DOSTĘPNE DODATKI---")
        for t in cls._toppings.values():
            print(f"  • {t}")
        print()


def clear():
    print("\n" * 2)


def pause():
    input("\n[Enter aby kontynuować...]")


def header(title: str):
    print("\n" + "=" * 30)
    print(f"{title}")
    print("=" * 30)


def init_catalog():
    for t in [
        Topping("Mozzarella", 2.0, is_vegan=False),
        Topping("Pepperoni", 3.5, is_vegan=False),
        Topping("Pieczarki", 1.5, is_vegan=True),
        Topping("Rukola", 1.0, is_vegan=True),
        Topping("Papryka", 1.5, is_vegan=True),
        Topping("Oliwki", 1.5, is_vegan=True),
    ]:
        ToppingCatalog.register(t)

    for p in [
        ClassicPizza("Margherita", 22.0, "średnia", sauce="pomidorowy"),
        ClassicPizza("Quattro Formaggi", 28.0, "średnia", sauce="śmietanowy"),
        VeganPizza("Green Power", 24.0, "średnia"),
        PremiumPizza(
            "Tartufo Nero", 45.0, "duża", chef_note="Pieczona w piecu opalanym drewnem"
        ),
        SeasonalPizza("Letnia Świeżość", 35.0, "duża", valid_until=date(2025, 8, 31)),
    ]:
        OrderManager.add_to_catalog(p)


def register_customer() -> Customer:
    header("REJESTRACJA KLIENTA")
    while True:
        try:
            first = input("Imię: ").strip()
            last = input("Nazwisko: ").strip()
            email = input("E-mail: ").strip()
            phone = input("Telefon: ").strip()
            age = int(input("Wiek : ").strip())
            customer = Customer(first, last, email, phone, age)
            print(f"\nWitaj, {customer.full_name}!")
            return customer
        except (ValueError, TypeError) as e:
            print(f"\nBłąd: {e}\n  Spróbuj ponownie.\n")


def choose_pizza_from_catalog() -> Optional[PizzaBase]:
    catalog = OrderManager._pizza_catalog
    if not catalog:
        print("Katalog jest pusty.")
        return None

    header("MENU PIZZ")
    items = list(catalog.values())
    for i, p in enumerate(items, 1):
        print(f"  {i}. {p.name:25s} | {p.pizza_type():15s} | od {p.get_price():.2f} zł")

    print("0. Anuluj")
    while True:
        try:
            choice = int(input("\nWybierz numer: "))
            if choice == 0:
                return None
            if 1 <= choice <= len(items):
                src = items[choice - 1]
                if isinstance(src, SeasonalPizza):
                    pizza = SeasonalPizza(
                        src.name,
                        src.base_price,
                        src.size,
                        src.valid_until,
                        src.chef_note,
                    )
                elif isinstance(src, PremiumPizza):
                    pizza = PremiumPizza(
                        src.name, src.base_price, src.size, src.chef_note
                    )
                elif isinstance(src, VeganPizza):
                    pizza = VeganPizza(src.name, src.base_price, src.size)
                else:
                    pizza = ClassicPizza(src.name, src.base_price, src.size, src._sauce)
                return pizza
            print("Nieprawidłowy numer.")
        except ValueError:
            print("Wpisz liczbę.")


def choose_size(pizza: PizzaBase) -> None:
    sizes = list(PizzaBase.VALID_SIZES.keys())
    print("\nRozmiary:")
    for i, s in enumerate(sizes, 1):
        mult = PizzaBase.VALID_SIZES[s]
        est = round(pizza.base_price * mult, 2)
        print(f"{i}. {s:10s} (mnożnik x{mult}, ~{est:.2f} zł)")
    while True:
        try:
            choice = int(input("Wybierz rozmiar [1-4]: "))
            if 1 <= choice <= len(sizes):
                pizza.size = sizes[choice - 1]
                print(f"Rozmiar: {pizza.size}")
                return
            print("Nieprawidłowy numer.")
        except ValueError:
            print("Wpisz liczbę.")


def choose_toppings(pizza: PizzaBase) -> None:
    header(f"DODATKI DO: {pizza.name}")
    toppings = list(ToppingCatalog._toppings.values())

    while True:
        print("\n Dostępne dodatki:")
        for i, t in enumerate(toppings, 1):
            print(f"  {i}. {t}")
        print(" 0. Gotowe")

        try:
            choice = int(input("\nDodaj topping (numer): "))
            if choice == 0:
                break
            if 1 <= choice <= len(toppings):
                t = toppings[choice - 1]
                try:
                    pizza.add_topping(t)
                    print(f"Dodano: {t.name}")
                except ValueError as e:
                    print(f"{e}")
            else:
                print("Nieprawidłowy numer.")
        except ValueError:
            print("Wpisz liczbę.")

    print(f"\nPizza: {pizza}")


def create_order(customer: Customer) -> Optional[Order]:
    header("NOWE ZAMÓWIENIE")
    order = Order(customer)
    order._delivery = False

    d = input("Dostawa do domu? (t/n): ").strip().lower()
    order._delivery = d == "t"

    notes = input("Uwagi do zamówienia (Enter = brak): ").strip()
    order._notes = notes

    while True:
        pizza = choose_pizza_from_catalog()
        if pizza is None:
            break

        choose_size(pizza)
        add_tops = input("Dodać własne dodatki? (t/n): ").strip().lower()
        if add_tops == "t":
            choose_toppings(pizza)

        order.add_item(pizza)
        print(f"\nDodano do zamówienia: {pizza}")

        again = input("\nDodać kolejną pizzę? (t/n): ").strip().lower()
        if again != "t":
            break

    if order.item_count() == 0:
        print("\nZamówienie jest puste – anulowano.")
        return None

    for p in order._pizzas:
        if isinstance(p, SeasonalPizza):
            disc = input(
                f"\nMasz kod rabatowy na '{p.name}'? Podaj % (0 = brak): "
            ).strip()
            try:
                pct = float(disc)
                if pct > 0:
                    new_price = p.apply_discount(pct)
                    print(f"Rabat {pct:.0f}% zastosowany → {new_price:.2f} zł")
            except ValueError:
                pass

    order.finalize()
    OrderManager.register_order(order)
    order.print_summary()
    return order


def show_history(customer: Customer):
    header("HISTORIA ZAMÓWIEŃ")
    history = customer.get_order_history()
    if not history:
        print("Brak zamówień.")
    else:
        print(f"{customer.full_name} – numery zamówień: {history}")
        matching = [o for o in OrderManager._all_orders if o.order_id in history]
        for o in matching:
            o.print_summary()


def main_menu(customer: Customer):
    while True:
        header(f"MENU GŁÓWNE  |  {customer.full_name}")
        print("1. Złóż nowe zamówienie")
        print("2. Przeglądaj menu pizz")
        print("3. Historia moich zamówień")
        print("4. Raport (wszystkie zamówienia)")
        print("6. Zmień dane klienta")
        print("0. Wyjście")

        choice = input("\n  Wybór: ").strip()

        if choice == "1":
            create_order(customer)
            pause()
        elif choice == "2":
            header("MENU PIZZ")
            for p in OrderManager._pizza_catalog.values():
                print(f"\n  {p.get_description()}")
            pause()
        elif choice == "3":
            show_history(customer)
            pause()
        elif choice == "4":
            OrderManager.print_report()
            pause()
        elif choice == "5":
            header("ZMIANA DANYCH")
            try:
                new_email = input(f"Nowy e-mail [{customer.email}]: ").strip()
                if new_email:
                    customer.email = new_email
                new_phone = input(f"Nowy telefon [{customer.phone}]: ").strip()
                if new_phone:
                    customer.phone = new_phone
                print("Dane zaktualizowane.")
            except ValueError as e:
                print(f"Błąd: {e}")
            pause()
        elif choice == "0":
            print("\nDo widzenia\n")
            break
        else:
            print("Nieprawidłowy wybór.")


if __name__ == "__main__":
    init_catalog()
    print("\nWitaj w pizzerii! Najpierw podaj swoje dane.\n")
    customer = register_customer()
    main_menu(customer)
