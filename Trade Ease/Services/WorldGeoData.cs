namespace TradeEase.Services;

using TradeEase.Models.Dtos;

internal static class WorldGeoData
{
    private static readonly (string Code, string Name, string Region)[] IsoCountries = new (string Code, string Name, string Region)[]
    {
        ("AF","Afghanistan","South Asia"),("AL","Albania","Europe"),("DZ","Algeria","Africa"),("AD","Andorra","Europe"),
        ("AO","Angola","Africa"),("AG","Antigua and Barbuda","Caribbean"),("AR","Argentina","South America"),("AM","Armenia","Asia"),
        ("AU","Australia","Oceania"),("AT","Austria","Europe"),("AZ","Azerbaijan","Asia"),("BS","Bahamas","Caribbean"),
        ("BH","Bahrain","Middle East"),("BD","Bangladesh","South Asia"),("BB","Barbados","Caribbean"),("BY","Belarus","Europe"),
        ("BE","Belgium","Europe"),("BZ","Belize","Central America"),("BJ","Benin","Africa"),("BT","Bhutan","South Asia"),
        ("BO","Bolivia","South America"),("BA","Bosnia and Herzegovina","Europe"),("BW","Botswana","Africa"),("BR","Brazil","South America"),
        ("BN","Brunei","Southeast Asia"),("BG","Bulgaria","Europe"),("BF","Burkina Faso","Africa"),("BI","Burundi","Africa"),
        ("CV","Cabo Verde","Africa"),("KH","Cambodia","Southeast Asia"),("CM","Cameroon","Africa"),("CA","Canada","North America"),
        ("CF","Central African Republic","Africa"),("TD","Chad","Africa"),("CL","Chile","South America"),("CN","China","East Asia"),
        ("CO","Colombia","South America"),("KM","Comoros","Africa"),("CG","Congo","Africa"),("CD","Democratic Republic of the Congo","Africa"),
        ("CR","Costa Rica","Central America"),("CI","Cote d'Ivoire","Africa"),("HR","Croatia","Europe"),("CU","Cuba","Caribbean"),
        ("CY","Cyprus","Europe"),("CZ","Czechia","Europe"),("DK","Denmark","Europe"),("DJ","Djibouti","Africa"),
        ("DM","Dominica","Caribbean"),("DO","Dominican Republic","Caribbean"),("EC","Ecuador","South America"),("EG","Egypt","Africa"),
        ("SV","El Salvador","Central America"),("GQ","Equatorial Guinea","Africa"),("ER","Eritrea","Africa"),("EE","Estonia","Europe"),
        ("SZ","Eswatini","Africa"),("ET","Ethiopia","Africa"),("FJ","Fiji","Oceania"),("FI","Finland","Europe"),
        ("FR","France","Europe"),("GA","Gabon","Africa"),("GM","Gambia","Africa"),("GE","Georgia","Asia"),
        ("DE","Germany","Europe"),("GH","Ghana","Africa"),("GR","Greece","Europe"),("GD","Grenada","Caribbean"),
        ("GT","Guatemala","Central America"),("GN","Guinea","Africa"),("GW","Guinea-Bissau","Africa"),("GY","Guyana","South America"),
        ("HT","Haiti","Caribbean"),("HN","Honduras","Central America"),("HK","Hong Kong","East Asia"),("HU","Hungary","Europe"),
        ("IS","Iceland","Europe"),("IN","India","South Asia"),("ID","Indonesia","Southeast Asia"),("IR","Iran","Middle East"),
        ("IQ","Iraq","Middle East"),("IE","Ireland","Europe"),("IL","Israel","Middle East"),("IT","Italy","Europe"),
        ("JM","Jamaica","Caribbean"),("JP","Japan","East Asia"),("JO","Jordan","Middle East"),("KZ","Kazakhstan","Central Asia"),
        ("KE","Kenya","Africa"),("KI","Kiribati","Oceania"),("KP","North Korea","East Asia"),("KR","South Korea","East Asia"),
        ("KW","Kuwait","Middle East"),("KG","Kyrgyzstan","Central Asia"),("LA","Laos","Southeast Asia"),("LV","Latvia","Europe"),
        ("LB","Lebanon","Middle East"),("LS","Lesotho","Africa"),("LR","Liberia","Africa"),("LY","Libya","Africa"),
        ("LI","Liechtenstein","Europe"),("LT","Lithuania","Europe"),("LU","Luxembourg","Europe"),("MO","Macau","East Asia"),
        ("MG","Madagascar","Africa"),("MW","Malawi","Africa"),("MY","Malaysia","Southeast Asia"),("MV","Maldives","South Asia"),
        ("ML","Mali","Africa"),("MT","Malta","Europe"),("MH","Marshall Islands","Oceania"),("MR","Mauritania","Africa"),
        ("MU","Mauritius","Africa"),("MX","Mexico","North America"),("FM","Micronesia","Oceania"),("MD","Moldova","Europe"),
        ("MC","Monaco","Europe"),("MN","Mongolia","East Asia"),("ME","Montenegro","Europe"),("MA","Morocco","Africa"),
        ("MZ","Mozambique","Africa"),("MM","Myanmar","Southeast Asia"),("NA","Namibia","Africa"),("NR","Nauru","Oceania"),
        ("NP","Nepal","South Asia"),("NL","Netherlands","Europe"),("NZ","New Zealand","Oceania"),("NI","Nicaragua","Central America"),
        ("NE","Niger","Africa"),("NG","Nigeria","Africa"),("MK","North Macedonia","Europe"),("NO","Norway","Europe"),
        ("OM","Oman","Middle East"),("PK","Pakistan","South Asia"),("PW","Palau","Oceania"),("PA","Panama","Central America"),
        ("PG","Papua New Guinea","Oceania"),("PY","Paraguay","South America"),("PE","Peru","South America"),("PH","Philippines","Southeast Asia"),
        ("PL","Poland","Europe"),("PT","Portugal","Europe"),("QA","Qatar","Middle East"),("RO","Romania","Europe"),
        ("RU","Russia","Europe/Asia"),("RW","Rwanda","Africa"),("KN","Saint Kitts and Nevis","Caribbean"),("LC","Saint Lucia","Caribbean"),
        ("VC","Saint Vincent and the Grenadines","Caribbean"),("WS","Samoa","Oceania"),("SM","San Marino","Europe"),("ST","Sao Tome and Principe","Africa"),
        ("SA","Saudi Arabia","Middle East"),("SN","Senegal","Africa"),("RS","Serbia","Europe"),("SC","Seychelles","Africa"),
        ("SL","Sierra Leone","Africa"),("SG","Singapore","Southeast Asia"),("SK","Slovakia","Europe"),("SI","Slovenia","Europe"),
        ("SB","Solomon Islands","Oceania"),("SO","Somalia","Africa"),("ZA","South Africa","Africa"),("SS","South Sudan","Africa"),
        ("ES","Spain","Europe"),("LK","Sri Lanka","South Asia"),("SD","Sudan","Africa"),("SR","Suriname","South America"),
        ("SE","Sweden","Europe"),("CH","Switzerland","Europe"),("SY","Syria","Middle East"),("TW","Taiwan","East Asia"),
        ("TJ","Tajikistan","Central Asia"),("TZ","Tanzania","Africa"),("TH","Thailand","Southeast Asia"),("TL","Timor-Leste","Southeast Asia"),
        ("TG","Togo","Africa"),("TO","Tonga","Oceania"),("TT","Trinidad and Tobago","Caribbean"),("TN","Tunisia","Africa"),
        ("TR","Turkey","Europe/Middle East"),("TM","Turkmenistan","Central Asia"),("TV","Tuvalu","Oceania"),("UG","Uganda","Africa"),
        ("UA","Ukraine","Europe"),("AE","United Arab Emirates","Middle East"),("GB","United Kingdom","Europe"),("US","United States","North America"),
        ("UY","Uruguay","South America"),("UZ","Uzbekistan","Central Asia"),("VU","Vanuatu","Oceania"),("VA","Vatican City","Europe"),
        ("VE","Venezuela","South America"),("VN","Vietnam","Southeast Asia"),("YE","Yemen","Middle East"),("ZM","Zambia","Africa"),
        ("ZW","Zimbabwe","Africa"),
    };

    public static string? TryGetCountryName(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        return IsoCountries.FirstOrDefault(c =>
            c.Code.Equals(code.Trim(), StringComparison.OrdinalIgnoreCase)).Name;
    }

    private static readonly Dictionary<string, string[]> KnownPorts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PK"] = new[]
        {
            "Port Qasim (Bin Qasim)", "Karachi Port (KPT)", "Muhammad Bin Qasim Terminal", "Gwadar Port",
            "Jinnah International Airport (Cargo)", "Lahore Dry Port", "Sialkot Dry Port", "Faisalabad Dry Port",
            "Peshawar Dry Port", "Quetta Dry Port", "Islamabad Dry Port", "Torkham Border (Land)", "Chaman Border (Land)",
        },
        ["AE"] = new[] { "Jebel Ali Port", "Khalifa Port (Abu Dhabi)", "Mina Rashid (Dubai)", "Sharjah Port", "Fujairah Port" },
        ["SA"] = new[] { "Jeddah Islamic Port", "King Abdul Aziz Port (Dammam)", "Yanbu Commercial Port", "Jubail Commercial Port", "Riyadh Dry Port" },
        ["CN"] = new[] { "Shanghai Port", "Shenzhen Port", "Ningbo-Zhoushan", "Guangzhou Port", "Qingdao Port", "Tianjin Port", "Xiamen Port" },
        ["IN"] = new[] { "Nhava Sheva (JNPT)", "Mundra Port", "Chennai Port", "Kolkata Port", "Cochin Port", "Visakhapatnam Port" },
        ["US"] = new[] { "Port of Los Angeles", "Port of Long Beach", "Port of New York/New Jersey", "Port of Houston", "Port of Savannah" },
        ["GB"] = new[] { "Felixstowe", "Southampton", "London Gateway", "Liverpool", "Immingham" },
        ["DE"] = new[] { "Hamburg", "Bremerhaven", "Frankfurt Dry Port" },
        ["FR"] = new[] { "Le Havre", "Marseille", "Dunkirk" },
        ["IT"] = new[] { "Genoa", "La Spezia", "Trieste", "Naples" },
        ["NL"] = new[] { "Rotterdam", "Amsterdam" },
        ["TR"] = new[] { "Ambarli (Istanbul)", "Mersin", "Izmir", "Gemlik" },
        ["MY"] = new[] { "Port Klang", "Tanjung Pelepas", "Penang" },
        ["SG"] = new[] { "Singapore Port", "Pasir Panjang Terminal" },
        ["TH"] = new[] { "Laem Chabang", "Bangkok Port" },
        ["ID"] = new[] { "Tanjung Priok (Jakarta)", "Surabaya", "Belawan" },
        ["JP"] = new[] { "Yokohama", "Tokyo", "Kobe", "Osaka" },
        ["KR"] = new[] { "Busan", "Incheon", "Gwangyang" },
        ["AU"] = new[] { "Melbourne", "Sydney", "Brisbane", "Fremantle" },
        ["BD"] = new[] { "Chittagong Port", "Mongla Port", "Dhaka ICD" },
        ["IR"] = new[] { "Bandar Abbas", "Imam Khomeini Port", "Chabahar" },
        ["QA"] = new[] { "Hamad Port", "Doha Port" },
        ["KW"] = new[] { "Shuwaikh Port", "Shuaiba Port" },
        ["OM"] = new[] { "Sohar Port", "Salalah Port", "Sultan Qaboos Port" },
        ["BH"] = new[] { "Khalifa Bin Salman Port" },
        ["EG"] = new[] { "Port Said", "Alexandria", "Damietta" },
        ["ZA"] = new[] { "Durban", "Cape Town", "Port Elizabeth" },
        ["KE"] = new[] { "Mombasa" },
        ["NG"] = new[] { "Lagos (Apapa)", "Port Harcourt", "Onne Port" },
        ["BR"] = new[] { "Santos", "Paranagua", "Rio de Janeiro" },
        ["CA"] = new[] { "Vancouver", "Montreal", "Halifax" },
        ["MX"] = new[] { "Manzanillo", "Veracruz", "Altamira" },
        ["RU"] = new[] { "Saint Petersburg", "Novorossiysk", "Vladivostok" },
        ["VN"] = new[] { "Ho Chi Minh (Cat Lai)", "Haiphong", "Da Nang" },
        ["PH"] = new[] { "Manila", "Cebu", "Davao" },
        ["LK"] = new[] { "Colombo", "Hambantota" },
        ["NP"] = new[] { "Birgunj Dry Port", "Kathmandu ICD" },
        ["AF"] = new[] { "Torkham Border", "Chaman Border", "Hairatan Border" },
        ["UZ"] = new[] { "Tashkent Dry Port", "Termez Border" },
        ["KZ"] = new[] { "Khorgos Dry Port", "Aktau Port" },
        ["HK"] = new[] { "Kwai Chung Container Terminal", "Hong Kong International Airport Cargo" },
        ["TW"] = new[] { "Kaohsiung Port", "Keelung Port", "Taichung Port" },
        ["PL"] = new[] { "Gdansk", "Gdynia" },
        ["ES"] = new[] { "Valencia", "Barcelona", "Algeciras" },
        ["PT"] = new[] { "Lisbon", "Leixoes", "Sines" },
        ["BE"] = new[] { "Antwerp", "Zeebrugge" },
        ["GR"] = new[] { "Piraeus", "Thessaloniki" },
        ["SE"] = new[] { "Gothenburg", "Stockholm" },
        ["NO"] = new[] { "Oslo", "Bergen" },
        ["DK"] = new[] { "Copenhagen", "Aarhus" },
        ["FI"] = new[] { "Helsinki", "HaminaKotka" },
        ["AT"] = new[] { "Vienna Dry Port" },
        ["CH"] = new[] { "Basel Rhine Port", "Geneva Airport Cargo" },
        ["IL"] = new[] { "Haifa", "Ashdod" },
        ["JO"] = new[] { "Aqaba" },
        ["LB"] = new[] { "Beirut Port" },
        ["IQ"] = new[] { "Umm Qasr", "Basra" },
        ["SY"] = new[] { "Latakia", "Tartus" },
        ["YE"] = new[] { "Aden", "Hodeidah" },
        ["MA"] = new[] { "Tangier Med", "Casablanca" },
        ["TN"] = new[] { "Rades", "Tunis" },
        ["DZ"] = new[] { "Algiers", "Oran", "Skikda" },
        ["ET"] = new[] { "Modjo Dry Port", "Addis Ababa ICD" },
        ["TZ"] = new[] { "Dar es Salaam", "Zanzibar" },
        ["GH"] = new[] { "Tema", "Takoradi" },
        ["CI"] = new[] { "Abidjan", "San Pedro" },
        ["SN"] = new[] { "Dakar" },
        ["AR"] = new[] { "Buenos Aires", "Rosario" },
        ["CL"] = new[] { "Valparaiso", "San Antonio" },
        ["CO"] = new[] { "Cartagena", "Buenaventura" },
        ["PE"] = new[] { "Callao" },
        ["EC"] = new[] { "Guayaquil" },
        ["PA"] = new[] { "Balboa", "Colon" },
        ["CR"] = new[] { "Limon", "Caldera" },
        ["NZ"] = new[] { "Auckland", "Tauranga" },
        ["MM"] = new[] { "Yangon", "Thilawa" },
        ["KH"] = new[] { "Sihanoukville", "Phnom Penh" },
        ["LA"] = new[] { "Vientiane Dry Port" },
        ["BN"] = new[] { "Muara Port" },
        ["MV"] = new[] { "Male Port" },
    };

    public static IReadOnlyList<PortDetailDto> GetPortsForCode(string code, string name, string? capital = null)
    {
        if (KnownPorts.TryGetValue(code, out var known))
            return known.Select(p => new PortDetailDto(p, PortType(p), null)).ToList();

        var ports = new List<PortDetailDto>
        {
            new($"{name} Main Seaport", "Sea", capital),
            new($"{name} International Airport (Cargo)", "Air", capital),
            new($"{name} Inland Container Depot", "Dry", capital),
        };
        return ports;
    }

    public static IReadOnlyList<CountryDetailDto> BuildAll() =>
        IsoCountries.Select(c => new CountryDetailDto(c.Code, c.Name, c.Region, Array.Empty<PortDetailDto>())).ToList();

    private static IReadOnlyList<PortDetailDto> BuildPorts(string code, string name) => GetPortsForCode(code, name);

    private static string PortType(string portName)
    {
        if (portName.Contains("Dry", StringComparison.OrdinalIgnoreCase) ||
            portName.Contains("ICD", StringComparison.OrdinalIgnoreCase) ||
            portName.Contains("Border", StringComparison.OrdinalIgnoreCase) ||
            portName.Contains("Land", StringComparison.OrdinalIgnoreCase))
            return "Dry";
        if (portName.Contains("Airport", StringComparison.OrdinalIgnoreCase)) return "Air";
        return "Sea";
    }
}
