namespace TradeEase.Services;

public record CatalogProduct(
    string Code,
    string Description,
    string Chapter,
    string Unit,
    decimal CustomsDutyPct,
    decimal SalesTaxPct,
    decimal AITPct,
    string TariffYear);

public static class WorldHsCatalogBuilder
{
    private static readonly Dictionary<string, string[]> ChapterProducts = new()
    {
        ["01"] = new[] { "Live horses", "Live bovine animals", "Live swine", "Live sheep and goats", "Live poultry", "Live fish" },
        ["02"] = new[] { "Beef fresh or chilled", "Beef frozen", "Poultry meat fresh", "Poultry meat frozen", "Pork meat", "Edible offal" },
        ["03"] = new[] { "Fish fillets fresh", "Fish frozen", "Shrimp and prawns", "Crabs and crustaceans", "Molluscs and squid" },
        ["04"] = new[] { "Milk and cream", "Cheese", "Butter", "Yogurt", "Eggs in shell", "Honey" },
        ["05"] = new[] { "Human hair", "Pigs bristles", "Guts and bladders", "Feathers and down" },
        ["06"] = new[] { "Cut flowers", "Live plants", "Bulbs and tubers" },
        ["07"] = new[] { "Potatoes fresh", "Tomatoes fresh", "Onions and shallots", "Cabbages", "Lettuce", "Carrots", "Cucumbers", "Garlic" },
        ["08"] = new[] { "Coconuts", "Brazil nuts", "Almonds", "Walnuts", "Apples", "Oranges fresh", "Bananas", "Grapes fresh", "Mangoes" },
        ["09"] = new[] { "Coffee roasted", "Green coffee", "Black tea", "Green tea", "Pepper", "Cumin seeds", "Turmeric", "Ginger" },
        ["10"] = new[] { "Wheat", "Rice semi-milled", "Maize", "Barley", "Oats", "Sorghum" },
        ["11"] = new[] { "Wheat flour", "Rice flour", "Cereal groats", "Starches" },
        ["12"] = new[] { "Soybeans", "Groundnuts", "Linseed", "Sunflower seeds", "Rape seeds", "Copra" },
        ["13"] = new[] { "Lac and gums", "Vegetable saps and extracts" },
        ["14"] = new[] { "Vegetable plaiting materials" },
        ["15"] = new[] { "Pig fat", "Olive oil", "Palm oil", "Sunflower oil", "Coconut oil", "Margarine" },
        ["16"] = new[] { "Sausages", "Prepared poultry meat", "Canned fish", "Crab prepared" },
        ["17"] = new[] { "Cane sugar", "Beet sugar", "Glucose syrup", "Cocoa powder with sugar" },
        ["18"] = new[] { "Cocoa beans", "Cocoa paste", "Chocolate bars" },
        ["19"] = new[] { "Malt extract food", "Pasta uncooked", "Bread and pastry", "Breakfast cereals", "Biscuits" },
        ["20"] = new[] { "Vegetables preserved", "Fruit jams", "Tomato paste", "Pickles", "Fruit juices" },
        ["21"] = new[] { "Extracts and sauces", "Ice cream", "Soups and broths" },
        ["22"] = new[] { "Waters and aerated", "Beer", "Wine", "Spirits and whisky", "Vinegar" },
        ["23"] = new[] { "Flours and meals of meat", "Oilcake animal feed", "Pet food" },
        ["24"] = new[] { "Unmanufactured tobacco", "Cigarettes", "Cigars" },
        ["25"] = new[] { "Salt", "Sulphur", "Kaolin clay", "Granite and sandstone", "Gypsum", "Marble" },
        ["26"] = new[] { "Iron ore", "Copper ore", "Aluminium ore", "Precious metal ores" },
        ["27"] = new[] { "Coal", "Petroleum crude", "Petroleum oils", "Natural gas", "Kerosene", "Lubricating oils" },
        ["28"] = new[] { "Sodium hydroxide", "Sulphuric acid", "Hydrogen peroxide", "Silicon dioxide" },
        ["29"] = new[] { "Hydrocarbons organic", "Acyclic alcohols", "Carboxylic acids", "Vitamins", "Antibiotics bulk" },
        ["30"] = new[] { "Pharmaceutical medicaments", "Vaccines", "Bandages and dressings", "Insulin" },
        ["31"] = new[] { "Fertilisers nitrogen", "Fertilisers phosphatic", "Fertilisers potassic" },
        ["32"] = new[] { "Tanning extracts", "Pigments and paints", "Printing ink", "Varnishes" },
        ["33"] = new[] { "Essential oils", "Perfumes", "Cosmetics", "Shampoo", "Soap" },
        ["34"] = new[] { "Soap organic", "Wax and polish", "Candles", "Lubricating preparations" },
        ["35"] = new[] { "Albumins", "Enzymes", "Modified starches", "Glues" },
        ["36"] = new[] { "Propellant powders", "Safety fuses", "Igniters" },
        ["37"] = new[] { "Photographic plates", "Photographic film", "Photographic chemicals" },
        ["38"] = new[] { "Miscellaneous chemical products", "Insecticides", "Diagnostic reagents", "Activated carbon" },
        ["39"] = new[] { "Polyethylene", "Polypropylene", "PVC", "Plastic plates and sheets", "Plastic tubes", "Plastic packaging" },
        ["40"] = new[] { "Natural rubber", "Synthetic rubber", "Rubber tyres new", "Rubber tubes", "Rubber footwear" },
        ["41"] = new[] { "Raw hides bovine", "Raw skins sheep", "Leather further prepared" },
        ["42"] = new[] { "Leather handbags", "Leather wallets", "Trunks and suitcases" },
        ["43"] = new[] { "Furskins", "Artificial fur" },
        ["44"] = new[] { "Fuel wood", "Wood in chips", "Sawn wood coniferous", "Plywood", "Wooden furniture parts" },
        ["45"] = new[] { "Natural cork", "Agglomerated cork" },
        ["46"] = new[] { "Plaiting materials", "Basketwork" },
        ["47"] = new[] { "Wood pulp", "Recovered paper" },
        ["48"] = new[] { "Kraft paper", "Corrugated paper board", "Toilet paper", "Cartons and boxes paper" },
        ["49"] = new[] { "Printed books", "Newspapers", "Maps and charts", "Postcards" },
        ["50"] = new[] { "Silk yarn", "Silk fabrics" },
        ["51"] = new[] { "Wool yarn", "Wool fabrics", "Carded wool" },
        ["52"] = new[] { "Cotton not carded", "Cotton yarn", "Cotton fabric plain", "Cotton denim", "Cotton bed linen" },
        ["53"] = new[] { "Flax yarn", "Jute and textile fibres" },
        ["54"] = new[] { "Synthetic filament yarn", "Synthetic woven fabrics" },
        ["55"] = new[] { "Synthetic staple fibres", "Polyester cotton yarn" },
        ["56"] = new[] { "Wadding and felt", "Twine and ropes", "Textile wipes" },
        ["57"] = new[] { "Carpets knotted", "Carpets tufted" },
        ["58"] = new[] { "Woven pile fabrics", "Embroidery", "Tapestries" },
        ["59"] = new[] { "Rubberised textile fabrics", "Conveyor belts textile", "Tent fabrics" },
        ["60"] = new[] { "Knitted fabrics pile", "Knitted fabrics elastic" },
        ["61"] = new[] { "Knitted jerseys and pullovers", "Knitted T-shirts", "Knitted trousers" },
        ["62"] = new[] { "Men woven shirts cotton", "Women woven trousers", "Coats and jackets woven" },
        ["63"] = new[] { "Blankets", "Bed linen cotton", "Curtains textile", "Used clothing" },
        ["64"] = new[] { "Waterproof footwear", "Leather footwear", "Rubber/plastic footwear", "Sports footwear" },
        ["65"] = new[] { "Hat forms", "Hats and headgear" },
        ["66"] = new[] { "Umbrellas", "Walking sticks" },
        ["67"] = new[] { "Prepared feathers", "Artificial flowers", "Human hair wigs" },
        ["68"] = new[] { "Setts and flagstones", "Grindstones", "Mineral wool insulation" },
        ["69"] = new[] { "Ceramic tiles glazed", "Ceramic sanitary fixtures", "Porcelain tableware" },
        ["70"] = new[] { "Glass containers", "Flat glass", "Safety glass", "Glass fibres" },
        ["71"] = new[] { "Pearls natural", "Diamonds", "Gold unwrought", "Silver unwrought", "Jewellery gold" },
        ["72"] = new[] { "Pig iron", "Ferro-alloys", "Flat rolled steel", "Bars and rods steel", "Steel wire" },
        ["73"] = new[] { "Sheet piling steel", "Tubes and pipes steel", "Structures steel", "Screws and bolts steel", "Steel drums" },
        ["74"] = new[] { "Copper refined cathodes", "Copper wire", "Copper tubes" },
        ["75"] = new[] { "Nickel unwrought", "Nickel alloys" },
        ["76"] = new[] { "Aluminium unwrought", "Aluminium plates", "Aluminium foil" },
        ["78"] = new[] { "Lead unwrought", "Lead plates" },
        ["79"] = new[] { "Zinc unwrought", "Zinc dust" },
        ["80"] = new[] { "Tin unwrought" },
        ["81"] = new[] { "Tungsten", "Molybdenum", "Magnesium" },
        ["82"] = new[] { "Hand tools", "Saws and blades", "Knives and blades", "Wrenches and spanners" },
        ["83"] = new[] { "Padlocks", "Mountings and fittings base metal", "Statuettes base metal" },
        ["84"] = new[] { "Nuclear reactors parts", "Pumps and compressors", "Computers laptops", "Computer parts", "Printers", "Centrifuges", "Air conditioning machines" },
        ["85"] = new[] { "Electric motors", "Transformers", "Telephone sets smartphones", "Network routers", "Television receivers", "Integrated circuits", "Electric conductors insulated", "Batteries lithium" },
        ["86"] = new[] { "Rail locomotives", "Railway freight wagons" },
        ["87"] = new[] { "Motor cars passenger", "Motor vehicles goods", "Motorcycles", "Tractors", "Vehicle parts and accessories" },
        ["88"] = new[] { "Aircraft civil", "Aircraft parts", "Parachutes" },
        ["89"] = new[] { "Cruise ships", "Cargo vessels", "Yachts and boats", "Floating structures" },
        ["90"] = new[] { "Optical fibres", "Lenses and prisms", "Medical instruments", "Surgical instruments", "Thermometers", "Measuring instruments" },
        ["91"] = new[] { "Wrist-watches", "Clock movements", "Watch cases" },
        ["92"] = new[] { "Pianos", "Musical instruments string", "Musical instruments wind" },
        ["93"] = new[] { "Military weapons parts", "Revolvers and pistols", "Ammunition" },
        ["94"] = new[] { "Seats and chairs", "Medical furniture", "Mattresses", "Lamps and lighting", "Wooden furniture", "Prefabricated buildings" },
        ["95"] = new[] { "Toys and models", "Video game consoles", "Sports equipment", "Festive articles" },
        ["96"] = new[] { "Worked ivory substitutes", "Combs and hairpins", "Toothbrushes", "Pens and markers", "Lighters" },
        ["97"] = new[] { "Paintings and drawings", "Antiques over 100 years" },
    };

    public static IReadOnlyList<CatalogProduct> Build()
    {
        var list = new List<CatalogProduct>(600);
        foreach (var (chapter, descriptions) in ChapterProducts.OrderBy(x => x.Key))
        {
            var (customs, sales, ait) = DutyForChapter(chapter);
            var unit = UnitForChapter(chapter);
            for (var i = 0; i < descriptions.Length; i++)
            {
                var code = $"{chapter}{(i + 1):D2}00";
                if (code.Length < 8) code = code.PadRight(8, '0');
                list.Add(new CatalogProduct(code, descriptions[i], chapter, unit, customs, sales, ait, "FY 2025-26"));
            }
        }
        return list;
    }

    public static (decimal Customs, decimal Sales, decimal Ait) DutyForChapter(string chapter)
    {
        if (!int.TryParse(chapter, out var ch)) return (10, 17, 5.5m);
        if (ch <= 5) return (0, 0, 1m);
        if (ch <= 14) return (5, 17, 5.5m);
        if (ch <= 24) return (15, 17, 5.5m);
        if (ch <= 27) return (10, 17, 5.5m);
        if (ch <= 38) return (5, 17, 5.5m);
        if (ch <= 40) return (20, 17, 5.5m);
        if (ch <= 43) return (25, 17, 5.5m);
        if (ch <= 49) return (10, 17, 5.5m);
        if (ch <= 63) return (20, 17, 5.5m);
        if (ch <= 64) return (25, 17, 5.5m);
        if (ch <= 70) return (20, 17, 5.5m);
        if (ch == 71) return (0, 17, 5.5m);
        if (ch <= 83) return (10, 17, 5.5m);
        if (ch <= 85) return (0, 17, 5.5m);
        if (ch <= 87) return (50, 17, 5.5m);
        if (ch <= 89) return (0, 17, 5.5m);
        if (ch <= 90) return (0, 17, 5.5m);
        return (20, 17, 5.5m);
    }

    public static string UnitForChapter(string chapter)
    {
        if (!int.TryParse(chapter, out var ch)) return "NOS";
        if (ch <= 5) return "NOS";
        if (ch <= 14) return "KGS";
        if (ch == 15) return "LTR";
        if (ch <= 24) return "KGS";
        if (ch <= 27) return "LTR";
        if (ch <= 39) return "KGS";
        if (ch <= 43) return "NOS";
        if (ch <= 49) return "KGS";
        if (ch <= 63) return "NOS";
        if (ch <= 64) return "PRS";
        if (ch <= 70) return "NOS";
        if (ch == 71) return "GRM";
        if (ch <= 73) return "KGS";
        if (ch <= 85) return "NOS";
        if (ch <= 87) return "NOS";
        return "NOS";
    }
}
