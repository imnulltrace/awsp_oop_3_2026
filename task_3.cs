using System;
using System.Collections.Generic;
using System.Linq;

public interface IDiscountable
{
    double ApplyDiscount(double percentage);
}

public class Topping
{
    private double _extraCost;

    public string Name { get; }
    public bool IsVegan { get; }

    public double ExtraCost
    {
        get => _extraCost;
        private set
        {
            if (value < 0) throw new ArgumentException($"Cena nie może być ujemna: {value}");
            _extraCost = value;
        }
    }

    public Topping(string name, double extraCost, bool isVegan = false)
    {
        Name = name;
        ExtraCost = extraCost;
        IsVegan = isVegan;
    }

    public override string ToString()
    {
        string veganTag = IsVegan ? "Tak" : "Nie";
        return $"{Name}{veganTag} (+{ExtraCost:F2} zł)";
    }
}

public abstract class PizzaBase
{
    public static readonly Dictionary<string, double> ValidSizes = new()
    {
        { "mała",    0.8 },
        { "średnia", 1.0 },
        { "duża",    1.3 },
        { "XXL",     1.6 },
    };

    private string _size;
    private double _basePrice;
    protected List<Topping> Toppings { get; } = new();

    public string Name { get; }

    public string Size
    {
        get => _size;
        set => _size = ValidateSize(value);
    }

    public double BasePrice
    {
        get => _basePrice;
        set => _basePrice = ValidatePrice(value);
    }

    protected PizzaBase(string name, double basePrice, string size = "średnia")
    {
        Name = name;
        _size = ValidateSize(size);
        _basePrice = ValidatePrice(basePrice);
    }

    protected static double ValidatePrice(double price)
    {
        if (price < 0) throw new ArgumentException($"Cena nie może być ujemna: {price}");
        return price;
    }

    private static string ValidateSize(string size)
    {
        if (!ValidSizes.ContainsKey(size))
            throw new ArgumentException(
                $"Nieprawidłowy rozmiar '{size}'. Dostępne: {string.Join(", ", ValidSizes.Keys)}");
        return size;
    }

    public virtual void AddTopping(Topping topping) => Toppings.Add(topping);

    public bool RemoveTopping(string name)
    {
        var t = Toppings.FirstOrDefault(x =>
            x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (t == null) return false;
        Toppings.Remove(t);
        return true;
    }

    public virtual double GetPrice()
    {
        double mult = ValidSizes[_size];
        double toppingsCost = Toppings.Sum(t => t.ExtraCost);
        return Math.Round((_basePrice + toppingsCost) * mult, 2);
    }

    public string ListToppings() =>
        Toppings.Count == 0 ? "bez dodatków" : string.Join(", ", Toppings.Select(t => t.Name));

    public abstract string PizzaType { get; }
    public abstract string GetDescription();

    public override string ToString() =>
        $"[{PizzaType}] {Name} ({_size}) | {ListToppings()} | {GetPrice():F2} zł";
}

public class ClassicPizza : PizzaBase
{
    private string _sauce;

    public string Sauce
    {
        get => _sauce;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Nazwa sosu nie może być pusta.");
            _sauce = value;
        }
    }

    public ClassicPizza(string name, double basePrice, string size = "średnia", string sauce = "pomidorowy")
        : base(name, basePrice, size)
    {
        _sauce = sauce;
    }

    public override string PizzaType => "Klasyczna";

    public override string GetDescription() =>
        $"Klasyczna pizza '{Name}' na sosie {_sauce}. " +
        $"Rozmiar: {Size}. Dodatki: {ListToppings()}. " +
        $"Cena: {GetPrice():F2} zł.";
}

public class VeganPizza : PizzaBase
{
    public VeganPizza(string name, double basePrice, string size = "średnia")
        : base(name, basePrice, size) { }

    public override void AddTopping(Topping topping)
    {
        if (!topping.IsVegan)
            throw new ArgumentException(
                $"Dodatek '{topping.Name}' nie jest wegański! " +
                "VeganPizza akceptuje tylko wegańskie składniki.");
        base.AddTopping(topping);
    }

    public override string PizzaType => "Wegańska";

    public override string GetDescription() =>
        $"Wegańska pizza '{Name}' - 100% roślinne składniki." +
        $"Rozmiar: {Size}. Dodatki: {ListToppings()}. " +
        $"Cena: {GetPrice():F2} zł.";
}

public class PremiumPizza : PizzaBase
{
    public string ChefNote { get; set; }

    public PremiumPizza(string name, double basePrice, string size = "duża", string chefNote = "")
        : base(name, basePrice, size)
    {
        ChefNote = chefNote;
    }

    public override string PizzaType => "Premium";

    public override string GetDescription()
    {
        string note = !string.IsNullOrEmpty(ChefNote) ? $" Notatka szefa: \"{ChefNote}\"." : "";
        return $"Pizza premium '{Name}'. " +
               $"Rozmiar: {Size}. Dodatki: {ListToppings()}. " +
               $"Cena: {GetPrice():F2} zł.{note}";
    }
}

public class SeasonalPizza : PremiumPizza, IDiscountable
{
    private double _discount;

    public DateOnly? ValidUntil { get; }
    public double Discount => _discount;

    public SeasonalPizza(string name, double basePrice, string size = "duża",
        DateOnly? validUntil = null, string chefNote = "")
        : base(name, basePrice, size, chefNote)
    {
        ValidUntil = validUntil;
    }

    public bool IsOfferActive() =>
        ValidUntil == null || DateOnly.FromDateTime(DateTime.Today) <= ValidUntil.Value;

    public double ApplyDiscount(double percentage)
    {
        if (percentage < 0 || percentage > 100)
            throw new ArgumentException("Procent rabatu musi być między 0 a 100.");
        _discount = percentage;
        return Math.Round(GetPrice() * (1 - percentage / 100.0), 2);
    }

    public override string PizzaType
    {
        get
        {
            string active = IsOfferActive() ? "Dostępna" : "WYGASŁA";
            return $"Sezonowa {active}";
        }
    }

    public override string GetDescription()
    {
        string desc = base.GetDescription();
        if (ValidUntil.HasValue)
            desc += $"Oferta ważna do: {ValidUntil.Value:dd.MM.yyyy}.";
        if (_discount > 0)
            desc += $"Rabat: {_discount:F0}% → {ApplyDiscount(_discount):F2} zł.";
        return desc;
    }
}

public class Customer
{
    private string _firstName;
    private string _lastName;
    private string _email;
    private string _phone = "";
    private int _age;
    private readonly List<int> _orderHistory = new();

    public string FirstName
    {
        get => _firstName;
        set
        {
            value = value.Trim();
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Imię nie może być puste.");
            _firstName = value;
        }
    }

    public string LastName
    {
        get => _lastName;
        set
        {
            value = value.Trim();
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("Nazwisko nie może być puste.");
            _lastName = value;
        }
    }

    public string FullName => $"{_firstName} {_lastName}";

    public string Email
    {
        get => _email;
        set
        {
            string v = value.Trim().ToLower();
            int at = v.IndexOf('@');
            if (at < 0 || !v[(at + 1)..].Contains('.'))
                throw new ArgumentException($"Nieprawidłowy adres e-mail: '{value}'");
            _email = v;
        }
    }

    public string Phone
    {
        get => _phone;
        set
        {
            if (!string.IsNullOrEmpty(value))
            {
                string digits = value.Replace(" ", "").Replace("-", "").Replace("+", "");
                if (!digits.All(char.IsDigit))
                    throw new ArgumentException($"Telefon zawiera niedozwolone znaki: '{value}'");
            }
            _phone = value;
        }
    }

    public int Age
    {
        get => _age;
        set
        {
            if (value <= 0 || value >= 130) throw new ArgumentException($"Nieprawidłowy wiek: {value}");
            _age = value;
        }
    }

    public Customer(string firstName, string lastName, string email, string phone = "", int age = 18)
    {
        _firstName = "";
        _lastName = "";
        _email = "";
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        Phone = phone;
        Age = age;
    }

    public void AddOrderToHistory(int orderId) => _orderHistory.Add(orderId);
    public List<int> GetOrderHistory() => new(_orderHistory);
    public int OrdersCount() => _orderHistory.Count;

    public override string ToString() =>
        $"Klient: {FullName} | Email: {_email} | Zamówień: {OrdersCount()}";
}

public class Order
{
    private static int _idCounter = 1000;
    private readonly List<PizzaBase> _pizzas = new();

    public int OrderId { get; }
    public Customer Customer { get; }
    public bool IsDelivery { get; set; }
    public string Notes { get; set; }
    public bool IsFinalized { get; private set; }
    public DateTime CreatedAt { get; } = DateTime.Now;

    internal IReadOnlyList<PizzaBase> Pizzas => _pizzas;

    public Order(Customer customer, bool delivery = true, string notes = "")
    {
        OrderId = ++_idCounter;
        Customer = customer;
        IsDelivery = delivery;
        Notes = notes;
    }

    public void AddItem(PizzaBase pizza)
    {
        if (IsFinalized) throw new InvalidOperationException("Nie można modyfikować sfinalizowanego zamówienia.");
        _pizzas.Add(pizza);
    }

    public bool RemoveItem(int index)
    {
        if (IsFinalized) throw new InvalidOperationException("Nie można modyfikować sfinalizowanego zamówienia.");
        if (index < 0 || index >= _pizzas.Count) return false;
        _pizzas.RemoveAt(index);
        return true;
    }

    public int ItemCount() => _pizzas.Count;

    public double CalculateTotal()
    {
        double fee = IsDelivery ? 5.0 : 0.0;
        return Math.Round(_pizzas.Sum(p => p.GetPrice()) + fee, 2);
    }

    public void Finalize()
    {
        if (_pizzas.Count == 0) throw new InvalidOperationException("Nie można złożyć pustego zamówienia.");
        IsFinalized = true;
        Customer.AddOrderToHistory(OrderId);
    }

    public void PrintSummary()
    {
        string sep = new('=', 20);
        string dash = new('-', 20);
        Console.WriteLine($"\n{sep}");
        Console.WriteLine($"ZAMÓWIENIE #{OrderId}");
        Console.WriteLine($"Klient: {Customer.FullName}");
        Console.WriteLine($"E-mail: {Customer.Email}");
        Console.WriteLine($"Data: {CreatedAt:dd.MM.yyyy HH:mm}");
        Console.WriteLine($"Dostawa: {(IsDelivery ? "TAK (+5.00 zł)" : "NIE (odbiór osobisty)")}");
        if (!string.IsNullOrEmpty(Notes)) Console.WriteLine($"  Uwagi: {Notes}");
        Console.WriteLine($"Status: {(IsFinalized ? "Złożone" : "W trakcie")}");
        Console.WriteLine(dash);

        if (_pizzas.Count == 0)
            Console.WriteLine("(brak pozycji)");
        else
            for (int i = 0; i < _pizzas.Count; i++)
                Console.WriteLine($"  {i + 1}. {_pizzas[i]}");

        Console.WriteLine(dash);
        double subtotal = _pizzas.Sum(p => p.GetPrice());
        Console.WriteLine($"Suma za pizze: {subtotal:F2} zł");
        if (IsDelivery) Console.WriteLine("Dostawa: 5.00 zł");
        Console.WriteLine($"RAZEM: {CalculateTotal():F2} zł");
        Console.WriteLine($"{sep}\n");
    }
}

public static class OrderManager
{
    private static readonly List<Order> AllOrders = new();
    private static readonly Dictionary<string, PizzaBase> PizzaCatalog = new();

    public static void RegisterOrder(Order order)
    {
        if (!order.IsFinalized) throw new ArgumentException("Można rejestrować tylko sfinalizowane zamówienia.");
        AllOrders.Add(order);
    }

    public static Order PlaceOrder(Customer customer, List<PizzaBase> pizzas,
        bool delivery = true, string notes = "")
    {
        var order = new Order(customer, delivery, notes);
        foreach (var p in pizzas) order.AddItem(p);
        order.Finalize();
        RegisterOrder(order);
        return order;
    }

    public static void AddToCatalog(PizzaBase pizza) =>
        PizzaCatalog[pizza.Name.ToLower()] = pizza;

    public static PizzaBase? GetFromCatalog(string name) =>
        PizzaCatalog.TryGetValue(name.ToLower(), out var p) ? p : null;

    public static IReadOnlyDictionary<string, PizzaBase> Catalog => PizzaCatalog;
    public static IReadOnlyList<Order> Orders => AllOrders;

    public static int CountByType(string pizzaType) =>
        AllOrders.SelectMany(o => o.Pizzas)
                 .Count(p => p.PizzaType.Contains(pizzaType, StringComparison.OrdinalIgnoreCase));

    public static void PrintReport()
    {
        string sep = new('=', 20);
        string dash = new('-', 20);
        Console.WriteLine($"\n{sep}");
        Console.WriteLine("RAPORT ZAMÓWIEŃ");
        Console.WriteLine(dash);
        Console.WriteLine($"Łączna liczba zamówień: {AllOrders.Count}");
        Console.WriteLine($"Łączny przychód: {AllOrders.Sum(o => o.CalculateTotal()):F2} zł");
        Console.WriteLine(dash);
        foreach (string t in new[] { "Klasyczna", "Wegańska", "Premium", "Sezonowa" })
        {
            int count = CountByType(t);
            if (count > 0) Console.WriteLine($"Pizze '{t}': {count} szt.");
        }
        Console.WriteLine($"{sep}\n");
    }
}

public static class ToppingCatalog
{
    private static readonly Dictionary<string, Topping> Toppings = new();

    public static void Register(Topping topping) =>
        Toppings[topping.Name.ToLower()] = topping;

    public static Topping Get(string name)
    {
        if (!Toppings.TryGetValue(name.ToLower(), out var t))
            throw new KeyNotFoundException($"Nie znaleziono dodatku '{name}' w katalogu.");
        return t;
    }

    public static IReadOnlyCollection<Topping> All => Toppings.Values;

    public static void ListAll()
    {
        Console.WriteLine("\n── DOSTĘPNE DODATKI ──");
        foreach (var t in Toppings.Values) Console.WriteLine($"  • {t}");
        Console.WriteLine();
    }
}

public static class UI
{
    public static void Pause() { Console.Write("\n[Enter aby kontynuować...]"); Console.ReadLine(); }

    public static void Header(string title)
    {
        Console.WriteLine("\n" + new string('=', 30));
        Console.WriteLine(title);
        Console.WriteLine(new string('=', 30));
    }

    public static Customer RegisterCustomer()
    {
        Header("REJESTRACJA KLIENTA");
        while (true)
        {
            try
            {
                Console.Write("Imię: "); string first = Console.ReadLine()!.Trim();
                Console.Write("Nazwisko: "); string last  = Console.ReadLine()!.Trim();
                Console.Write("E-mail: "); string email = Console.ReadLine()!.Trim();
                Console.Write("Telefon: "); string phone = Console.ReadLine()!.Trim();
                Console.Write("Wiek: "); int age = int.Parse(Console.ReadLine()!.Trim());
                var customer = new Customer(first, last, email, phone, age);
                Console.WriteLine($"\nWitaj, {customer.FullName}!");
                return customer;
            }
            catch (Exception e)
            {
                Console.WriteLine($"\nBłąd: {e.Message}\n  Spróbuj ponownie.\n");
            }
        }
    }

    public static PizzaBase? ChoosePizzaFromCatalog()
    {
        var catalog = OrderManager.Catalog;
        if (catalog.Count == 0) { Console.WriteLine("Katalog jest pusty."); return null; }

        Header("MENU PIZZ");
        var items = catalog.Values.ToList();
        for (int i = 0; i < items.Count; i++)
            Console.WriteLine($"  {i + 1}. {items[i].Name,-25} | {items[i].PizzaType,-15} | od {items[i].GetPrice():F2} zł");
        Console.WriteLine("0. Anuluj");

        while (true)
        {
            Console.Write("\nWybierz numer: ");
            if (!int.TryParse(Console.ReadLine(), out int choice)) { Console.WriteLine("Wpisz liczbę."); continue; }
            if (choice == 0) return null;
            if (choice < 1 || choice > items.Count) { Console.WriteLine("Nieprawidłowy numer."); continue; }

            var src = items[choice - 1];
            return src switch
            {
                SeasonalPizza s => new SeasonalPizza(s.Name, s.BasePrice, s.Size, s.ValidUntil, s.ChefNote),
                PremiumPizza  p => new PremiumPizza(p.Name,  p.BasePrice, p.Size, p.ChefNote),
                VeganPizza    v => new VeganPizza(v.Name,    v.BasePrice, v.Size),
                ClassicPizza  c => new ClassicPizza(c.Name,  c.BasePrice, c.Size, c.Sauce),
                _             => throw new InvalidOperationException("Nieznany typ pizzy.")
            };
        }
    }

    public static void ChooseSize(PizzaBase pizza)
    {
        var sizes = PizzaBase.ValidSizes.Keys.ToList();
        Console.WriteLine("\nRozmiary:");
        for (int i = 0; i < sizes.Count; i++)
        {
            double mult = PizzaBase.ValidSizes[sizes[i]];
            double est  = Math.Round(pizza.BasePrice * mult, 2);
            Console.WriteLine($"{i + 1}. {sizes[i],-10} (mnożnik x{mult}, ~{est:F2} zł)");
        }
        while (true)
        {
            Console.Write("Wybierz rozmiar [1-4]: ");
            if (int.TryParse(Console.ReadLine(), out int c) && c >= 1 && c <= sizes.Count)
            {
                pizza.Size = sizes[c - 1];
                Console.WriteLine($"Rozmiar: {pizza.Size}");
                return;
            }
            Console.WriteLine("Nieprawidłowy numer.");
        }
    }

    public static void ChooseToppings(PizzaBase pizza)
    {
        Header($"DODATKI DO: {pizza.Name}");
        var toppings = ToppingCatalog.All.ToList();
        while (true)
        {
            Console.WriteLine("\nDostępne dodatki:");
            for (int i = 0; i < toppings.Count; i++) Console.WriteLine($"  {i + 1}. {toppings[i]}");
            Console.WriteLine(" 0. Gotowe");
            Console.Write("\nDodaj topping (numer): ");
            if (!int.TryParse(Console.ReadLine(), out int c)) { Console.WriteLine("Wpisz liczbę."); continue; }
            if (c == 0) break;
            if (c < 1 || c > toppings.Count) { Console.WriteLine("Nieprawidłowy numer."); continue; }
            try { pizza.AddTopping(toppings[c - 1]); Console.WriteLine($"Dodano: {toppings[c - 1].Name}"); }
            catch (ArgumentException e) { Console.WriteLine(e.Message); }
        }
        Console.WriteLine($"\nPizza: {pizza}");
    }

    public static Order? CreateOrder(Customer customer)
    {
        Header("NOWE ZAMÓWIENIE");
        var order = new Order(customer);

        Console.Write("Dostawa do domu? (t/n): ");
        order.IsDelivery = Console.ReadLine()?.Trim().ToLower() == "t";

        Console.Write("Uwagi do zamówienia (Enter = brak): ");
        order.Notes = Console.ReadLine()?.Trim() ?? "";

        while (true)
        {
            var pizza = ChoosePizzaFromCatalog();
            if (pizza == null) break;

            ChooseSize(pizza);

            Console.Write("Dodać własne dodatki? (t/n): ");
            if (Console.ReadLine()?.Trim().ToLower() == "t") ChooseToppings(pizza);

            order.AddItem(pizza);
            Console.WriteLine($"\nDodano do zamówienia: {pizza}");

            Console.Write("\nDodać kolejną pizzę? (t/n): ");
            if (Console.ReadLine()?.Trim().ToLower() != "t") break;
        }

        if (order.ItemCount() == 0) { Console.WriteLine("\nZamówienie jest puste - anulowano."); return null; }

        foreach (var p in order.Pizzas.OfType<SeasonalPizza>())
        {
            Console.Write($"\nMasz kod rabatowy na '{p.Name}'? Podaj % (0 = brak): ");
            if (double.TryParse(Console.ReadLine(), out double pct) && pct > 0)
            {
                double newPrice = p.ApplyDiscount(pct);
                Console.WriteLine($"Rabat {pct:F0}% zastosowany → {newPrice:F2} zł");
            }
        }

        order.Finalize();
        OrderManager.RegisterOrder(order);
        order.PrintSummary();
        return order;
    }

    public static void ShowHistory(Customer customer)
    {
        Header("HISTORIA ZAMÓWIEŃ");
        var history = customer.GetOrderHistory();
        if (history.Count == 0) { Console.WriteLine("Brak zamówień."); return; }
        Console.WriteLine($"{customer.FullName} - numery zamówień: [{string.Join(", ", history)}]");
        foreach (var o in OrderManager.Orders.Where(o => history.Contains(o.OrderId)))
            o.PrintSummary();
    }
}

public static class Program
{
    static void InitCatalog()
    {
        foreach (var t in new Topping[]
        {
            new("Mozzarella", 2.0,  isVegan: false),
            new("Pepperoni",  3.5,  isVegan: false),
            new("Pieczarki",  1.5,  isVegan: true),
            new("Rukola",     1.0,  isVegan: true),
            new("Papryka",    1.5,  isVegan: true),
            new("Oliwki",     1.5,  isVegan: true),
        }) ToppingCatalog.Register(t);

        foreach (var p in new PizzaBase[]
        {
            new ClassicPizza("Margherita",       22.0, "średnia", sauce: "pomidorowy"),
            new ClassicPizza("Quattro Formaggi", 28.0, "średnia", sauce: "śmietanowy"),
            new VeganPizza  ("Green Power",      24.0, "średnia"),
            new PremiumPizza("Tartufo Nero",     45.0, "duża",    chefNote: "Pieczona w piecu opalanym drewnem"),
            new SeasonalPizza("Letnia Świeżość", 35.0, "duża",    validUntil: new DateOnly(2025, 8, 31)),
        }) OrderManager.AddToCatalog(p);
    }

    static void MainMenu(Customer customer)
    {
        while (true)
        {
            UI.Header($"MENU GŁÓWNE  |  {customer.FullName}");
            Console.WriteLine("1. Złóż nowe zamówienie");
            Console.WriteLine("2. Przeglądaj menu pizz");
            Console.WriteLine("3. Historia moich zamówień");
            Console.WriteLine("4. Raport (wszystkie zamówienia)");
            Console.WriteLine("5. Zmień dane klienta");
            Console.WriteLine("0. Wyjście");
            Console.Write("\n  Wybór: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                    UI.CreateOrder(customer);
                    UI.Pause();
                    break;
                case "2":
                    UI.Header("MENU PIZZ");
                    foreach (var p in OrderManager.Catalog.Values)
                        Console.WriteLine($"\n  {p.GetDescription()}");
                    UI.Pause();
                    break;
                case "3":
                    UI.ShowHistory(customer);
                    UI.Pause();
                    break;
                case "4":
                    OrderManager.PrintReport();
                    UI.Pause();
                    break;
                case "5":
                    UI.Header("ZMIANA DANYCH");
                    try
                    {
                        Console.Write($"Nowy e-mail [{customer.Email}]: ");
                        string? e = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(e)) customer.Email = e;

                        Console.Write($"Nowy telefon [{customer.Phone}]: ");
                        string? ph = Console.ReadLine()?.Trim();
                        if (!string.IsNullOrEmpty(ph)) customer.Phone = ph;

                        Console.WriteLine("Dane zaktualizowane.");
                    }
                    catch (Exception ex) { Console.WriteLine($"Błąd: {ex.Message}"); }
                    UI.Pause();
                    break;
                case "0":
                    Console.WriteLine("\nDo widzenia\n");
                    return;
                default:
                    Console.WriteLine("Nieprawidłowy wybór.");
                    break;
            }
        }
    }

    public static void Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        InitCatalog();
        Console.WriteLine("\nWitaj w pizzerii! Najpierw podaj swoje dane.\n");
        var customer = UI.RegisterCustomer();
        MainMenu(customer);
    }
}
