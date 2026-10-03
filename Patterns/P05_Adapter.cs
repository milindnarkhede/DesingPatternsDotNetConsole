namespace ShopKart.Patterns.Adapter;

// ===== What OUR app wants (TARGET interface) =====
public record Shipment(string OrderId, decimal WeightKg, string Pincode);
public record TrackingInfo(string Courier, string TrackingNo, int EtaDays);

public interface IShippingProvider
{
    TrackingInfo Ship(Shipment shipment);
}

// ===== Third-party SDKs we CANNOT change (ADAPTEES) =====
// FastShip: old SOAP-style SDK. Wants grams inside XML, replies "OK|trackingNo|days"
public class FastShipLegacyClient
{
    public string CreateConsignment(string xml)
    {
        Out.Info($"[FastShip SDK] received: {xml}");
        string grams = Between(xml, "<grams>", "</grams>");
        string pin   = Between(xml, "<pin>", "</pin>");
        int days = pin.StartsWith("41") ? 2 : 5;
        return $"OK|FS{pin}{grams}|{days}";
    }
    private static string Between(string s, string a, string b)
    {
        int start = s.IndexOf(a) + a.Length;
        return s[start..s.IndexOf(b)];
    }
}

// BlueParcel: US company. Wants pounds and "zip", returns its own receipt type
public record BlueParcelReceipt(string Awb, int TransitDays);
public class BlueParcelApi
{
    public BlueParcelReceipt Book(string reference, double pounds, string zip)
    {
        Out.Info($"[BlueParcel API] Book(ref={reference}, lbs={pounds:0.00}, zip={zip})");
        return new BlueParcelReceipt($"BP-{reference}", 3);
    }
}

// ===== ADAPTERS: translate our call into each vendor's language =====
public class FastShipAdapter(FastShipLegacyClient client) : IShippingProvider
{
    public TrackingInfo Ship(Shipment s)
    {
        int grams = (int)(s.WeightKg * 1000);                                   // kg → grams
        string xml = $"<consignment><ref>{s.OrderId}</ref><grams>{grams}</grams><pin>{s.Pincode}</pin></consignment>";
        string[] parts = client.CreateConsignment(xml).Split('|');             // "OK|FS...|2"
        if (parts[0] != "OK") throw new InvalidOperationException("FastShip rejected the shipment");
        return new TrackingInfo("FastShip", parts[1], int.Parse(parts[2]));
    }
}

public class BlueParcelAdapter(BlueParcelApi api) : IShippingProvider
{
    public TrackingInfo Ship(Shipment s)
    {
        double pounds = (double)s.WeightKg * 2.20462;                           // kg → lbs
        BlueParcelReceipt r = api.Book(s.OrderId, pounds, s.Pincode);
        return new TrackingInfo("BlueParcel", r.Awb, r.TransitDays);            // their type → ours
    }
}

// ===== Our code depends ONLY on IShippingProvider =====
public class DispatchService(IShippingProvider courier)
{
    public void Dispatch(Shipment s)
    {
        Out.Step($"Dispatch {s.OrderId}: {s.WeightKg} kg to {s.Pincode}");
        TrackingInfo t = courier.Ship(s);
        Out.Ok($"{t.Courier} tracking {t.TrackingNo}, arrives in {t.EtaDays} days");
    }
}

public static class AdapterDemo
{
    public static void Run()
    {
        Out.Title("05 ADAPTER — plug any courier into ShopKart");
        var shipment = new Shipment("SK-1001", 1.5m, "411001");

        Out.Section("Using FastShip (grams + XML)");
        new DispatchService(new FastShipAdapter(new FastShipLegacyClient())).Dispatch(shipment);

        Out.Section("Switch to BlueParcel (pounds + zip): DispatchService unchanged");
        new DispatchService(new BlueParcelAdapter(new BlueParcelApi())).Dispatch(shipment);
    }
}
