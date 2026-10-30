// İngilizce oyun metinleri. Türkçe asıl veriler kendi dosyalarında durur;
// burası her kaydın İngilizce karşılığını (aynı Id ile) doldurur.
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public static class English
    {
        // ===================== Elementler (atom numarası sırasıyla) =====================
        static readonly string[] ElementNames =
        {
            "Hydrogen","Helium","Lithium","Beryllium","Boron","Carbon","Nitrogen","Oxygen","Fluorine","Neon",
            "Sodium","Magnesium","Aluminium","Silicon","Phosphorus","Sulfur","Chlorine","Argon","Potassium","Calcium",
            "Scandium","Titanium","Vanadium","Chromium","Manganese","Iron","Cobalt","Nickel","Copper","Zinc",
            "Gallium","Germanium","Arsenic","Selenium","Bromine","Krypton","Rubidium","Strontium","Yttrium","Zirconium",
            "Niobium","Molybdenum","Technetium","Ruthenium","Rhodium","Palladium","Silver","Cadmium","Indium","Tin",
            "Antimony","Tellurium","Iodine","Xenon","Caesium","Barium","Lanthanum","Cerium","Praseodymium","Neodymium",
            "Promethium","Samarium","Europium","Gadolinium","Terbium","Dysprosium","Holmium","Erbium","Thulium","Ytterbium",
            "Lutetium","Hafnium","Tantalum","Tungsten","Rhenium","Osmium","Iridium","Platinum","Gold","Mercury",
            "Thallium","Lead","Bismuth","Polonium","Astatine","Radon","Francium","Radium","Actinium","Thorium",
            "Protactinium","Uranium","Neptunium","Plutonium","Americium","Curium","Berkelium","Californium","Einsteinium","Fermium",
            "Mendelevium","Nobelium","Lawrencium","Rutherfordium","Dubnium","Seaborgium","Bohrium","Hassium","Meitnerium","Darmstadtium",
            "Roentgenium","Copernicium","Nihonium","Flerovium","Moscovium","Livermorium","Tennessine","Oganesson"
        };

        public static void ApplyElements(IEnumerable<ElementData> all)
        {
            foreach (var e in all) if (e.Number >= 1 && e.Number <= ElementNames.Length) e.NameEn = ElementNames[e.Number - 1];
        }

        public static string Category(ElementCategory c)
        {
            switch (c)
            {
                case ElementCategory.Nonmetal: return "Nonmetal";
                case ElementCategory.NobleGas: return "Noble gas";
                case ElementCategory.AlkaliMetal: return "Alkali metal";
                case ElementCategory.AlkalineEarth: return "Alkaline earth";
                case ElementCategory.Metalloid: return "Metalloid";
                case ElementCategory.Halogen: return "Halogen";
                case ElementCategory.PostTransition: return "Post-transition metal";
                case ElementCategory.Transition: return "Transition metal";
                case ElementCategory.Lanthanide: return "Lanthanide";
                default: return "Actinide";
            }
        }

        // ===================== Simya adları ve hikâyeleri =====================
        // Biçim: "Alchemy name|Lore"
        public static readonly string[] Alchemy =
        {
            "Pure Dew Essence|A shimmering drop of water gathered from leaves at dawn",
            "Sun's Breath|A light, floating flask with golden gas swirling inside",
            "Stone of Calm|A pale pink-grey pebble found in still riverbeds",
            "Emerald Seed|A hard, greenish crystal that grows out of mossy rocks",
            "Barren Earth Salt|A bright white crystal gathered from dried-up lake beds",
            "Black Coal Essence|A black stone taken from the smithy forge or burnt ancient trees",
            "Choking Mist Flask|Cold, grey-blue smoke bottled from the surface of the swamp",
            "Wind of Life|A clear, bubbling orb filled at the foot of the waterfall",
            "Rotten Tooth Crystal|A yellow-green cave mineral that eats away whatever touches it",
            "Red Lantern Spirit|A rare swamp gas that glows orange-red at night",
            "Weeping Rock Salt|A white rock that fizzes and sparks when it touches water",
            "Blinding Leaf Dust|A silvery leaf that bursts into dazzling white light in a fire",
            "Silver Clay|A light metal filtered from the red riverbank soil (bauxite)",
            "Clear Quartz Sand|Glittering creek sand the glassblower uses for his flasks",
            "Ghost Light Dust|Graveyard mushroom essence that glows faint green in the dark",
            "Hellfire Yellow|A sharp-smelling yellow stone that gathers around hot springs",
            "Swamp Poison Gas|A dense, burning yellow-green alchemical smoke",
            "Lazy Air|A heavy, purple-glowing orb that refuses to react with anything",
            "Ash of Plenty|A purple-flamed salt filtered from oak ash at the woodcutter's camp",
            "Chalk Bone Stone|Milk-white limestone broken off the white cliffs",
            "North Wind Ore|A rare ore found only on cold rocks high in the hills",
            "Titan Ore|A shiny dark-grey chunk of rock that is almost impossible to break",
            "Beauty Crystal|A many-coloured stone that turns yellow, blue and violet when melted",
            "Mirror Ore|A metal that shines as if polished, used by the painter",
            "Purple Steel Stone|A black-brown veined rock that dyes glass purple",
            "Blood Earth Ore|A heavy, magnetic dark ore covered in red rust",
            "Midnight Blue Goblin Stone|A deep blue ore miners say the kobold goblins hide",
            "False Copper (Devil's Copper)|A greenish metal that looks like copper but is hard to forge",
            "Red Spark Ore|A red-orange ingot patched here and there with green patina",
            "Ridged Grey Stone|A bluish-grey metal that keeps roofs and shields from rusting",
            "Palm-Melting Silver|A metal that is solid when cold but melts in Aqualf's warm hand",
            "Grey Crystal Eye|A shiny semi-metal stone that bends light in strange ways",
            "Rat-Bane Dust|A layered yellow-grey crystal used to brew deadly poisons",
            "Moonlight Dust|A silver-red mineral that only glows under a full moon",
            "Red Ichor|A heavy, foul-smelling blood-red liquid kept in a thick glass bottle",
            "Gas of Hidden Secrets|A white-green glowing gas from sealed jars in old ruins",
            "Ruby Ash|A dark red salt that bursts in red-purple flashes when it meets water",
            "Red Flame Stone|A mineral that gives fireworks and flares their blood-red colour",
            "Dwarf Cave Stone|A grey-black crystal that twinkles like stars in deep tunnels",
            "False Diamond|A clear stone as bright and fireproof as a real diamond",
            "Weeping Goddess Metal|A grey metal with thin rainbow-like oxide sheens",
            "Pencil Rock|An ore that looks like lead but never melts, even in the fiercest fire",
            "Artificial Ghost Dust|A glimmer found nowhere in nature, only as a by-product of the cauldron",
            "Hardening Silver|A rare black-silver powder that makes gold and platinum hard as armour",
            "Rose Glimmer|A precious ore that never fades or tarnishes and shines pink-silver",
            "Poison-Drinking Sponge Metal|A porous white metal that soaks up foul gases and spells",
            "Pure Moon Tear|A bright white ingot that keeps werewolves and dark curses away",
            "Poison Yellow Paint Stone|A dangerous, vivid yellow-orange mineral used by painters",
            "Screaming Tin|A soft metal that gives a thin 'creak' or 'scream' when bent",
            "Tinplate Ore|A shiny, easy-melting metal mixed with copper to make bronze",
            "Kohl Stone|A star-patterned black metallic crystal old wizards wore around their eyes",
            "Earth Spirit Ore|A silvery stone that gives off garlicky smoke and bonds with gold",
            "Violet Vapour Crystal|Black-purple flakes that turn straight into deep purple smoke when heated",
            "Foreign Blue Orb|A very heavy gas flask with lightning-blue sparks crackling inside",
            "Liquid Gold Drop|A golden ampoule that melts at room temperature and is wildly explosive",
            "Heavy Green Flame Stone|A white stone with a green flame, far heavier than it looks",
            "Hidden Root Ore|A stone that hides inside other ores and darkens in the air",
            "Flint Spark|A metallic chip that showers sparks when scraped with a knife",
            "Leek-Green Glass Stone|A rare crystal that gives glass and potion bottles a leek-green tint",
            "Colour-Changing Magnet|A strong magnet stone, yellow in daylight and purple by candlelight",
            "Stolen Fire Embers|Warm radioactive embers that never go out and glow blue-green in the dark",
            "Brittle Magnet|A pale yellow ore that keeps its pull even at very high heat",
            "Red Glow-in-the-Dark Dust|A rare phosphor dust that glows blood-red under magic light (UV)",
            "Cold Magnet Stone|A metal that only becomes magnetic in winter or when chilled with ice potion",
            "Emerald Glow Dust|A charm dust that gives off the brightest green light in the dark",
            "Hard-to-Reach Ore|A heavy ore found only at the bottom of the deepest, thorniest cliffs",
            "Polar Pull Stone|A yellowish crystal with a stronger magnetic field than any known ore",
            "Pink Potion Crystal|An ore that gives potions and glass a sweet pink that never fades",
            "Last Stone of the North|A stone from misty mountain peaks that gives off portable X-ray light",
            "Quake-Sensing Crystal|A silvery stone that makes electricity when the ground shakes or it is squeezed",
            "White Stone of Paris|The heaviest, hardest and rarest of the rare earths",
            "Spell-Eating Shield Stone|A heavy dark-grey metal that soaks up magic (neutrons) like a sponge",
            "Cursed Thirst Metal|An undying blue-grey metal that even the strongest acid potions cannot melt",
            "Wolf Froth (Wolfram)|A very heavy black ore that is nearly impossible to melt and 'eats' tin",
            "River Spirit Ore|A heat-proof silvery powder found once in a million in riverbeds",
            "Sharp-Smelling Heavy Stone|The densest matter in the world; a bluish metal with a sharp, smoky smell",
            "Meteor Rainbow|A metal from fallen meteors that gives rainbow-coloured salts",
            "Noble White Gold|A silvery ingot used in royal seals and the purest alchemy cauldrons",
            "Flawless Sun Ingot|The final goal of alchemy: a pure yellow metal that never rusts or rots",
            "Living Silver (Quicksilver)|A heavy, poisonous silver liquid that scurries around the flask as if alive",
            "Green Sprout Poison|An assassin's powder with no smell or taste that leaves an emerald line in fire",
            "Saturn's Heavy Metal|A dull grey ingot alchemists tried to turn into gold; magic can't pass through it",
            "Stepped Rainbow Crystal|An iridescent metal that grows itself into geometric staircases",
            "Self-Heating Ember|A deadly, always-warm powder with a blue halo (alpha radiation)",
            "Fleeting Shadow Crystal|An unstable black crystal that evaporates the moment it appears",
            "Cellar Ghost Gas|Invisible, heavy radioactive smoke that pools in old stone cellars",
            "Instant Spark Dust|A powder that lasts only minutes and blows up the cauldron the moment it touches water",
            "Cursed Green Glow|A paint stone that glows a bewitching emerald at night but poisons whoever touches it",
            "Blue Ray Stone|A silvery ore that casts a faint blue beam in dark caves",
            "Thunder God's Rock|A long-lasting energy stone that flashes like lightning when struck",
            "First Ray Ore|A very rare, shiny metallic-grey and heavy charm stone",
            "Yellowcake Ore (Sky Stone)|A heavy ore packed with energy that makes glass glow green-yellow",
            "Deep Ocean Ore|A metal formed in sunken ships, resonating with the water element",
            "Underworld Lord's Embers|A warm core that blows up the whole lab if mixed the wrong way",
            "Smoke Watcher Crystal|A charmed metal that warns you when smoke or fire breaks out in the lab",
            "Purple Ember Crystal|A hot stone that bubbles with its own energy and glows purple-red in the dark",
            "West Academy Dust|A silver-white powder that only great wizard towers can make",
            "Star Core Shard|An overpowered crystal; a single grain can boil a whole cauldron",
            "Time-Bending Crystal|A blue glimmer whose own energy shatters its crystal shape",
            "Firestorm Ashes|A microscopic dust found only in the ashes of great alchemy blasts (Fails)",
            "Order-Maker Dust|An essence measured atom by atom, used in the spell that puts elements in order",
            "Powder Master Crystal|An unstable spark that doubles the power of explosive potions",
            "End-of-Cycle Stone|A metal that marks the border between green nature magic and dark alchemy",
            "Nucleus Hunter Essence|A red energy orb with a tiny atomic nucleus spinning inside",
            "Twin River Spirit|An unstable drop made by smashing together the waters of two magic rivers",
            "Essence of the Alchemist's Sea|An ocean-blue plasma only the wisest living alchemist can make",
            "Orbit Ring|An orb that defies gravity with rings of light spinning around it",
            "Silver Forest Spirit|A heavy, fleeting metallic vapour that evaporates in seconds",
            "Splitting Soul Crystal|A crystal that sends out a huge wave of magic when split in two",
            "Alchemy Furnace Spark|A brief glimmer born only when gold and nickel are fused by magic",
            "Light That Sees the Unseen|A ghostly golden essence that shows what lies behind walls and solid things",
            "Sun-Centred Mercury|An orb like a tiny solar system that acts like both a gas and a liquid metal",
            "Essence of the Rising Sun|A drop of red light from the ancient alchemy scrolls of the eastern lands",
            "Lead Ghost|A magic smoke as heavy as lead that still drifts away like a noble gas",
            "Red Tower Crystal|A ruby glimmer that vanishes in a tenth of a second, like a blinking eye",
            "Sulfur Lord Essence|The purest, highest form of energy of sulfur and hellstones",
            "Seventh Seal Salt|An unstable black-gold crystal, ancestor of all salts and halogens",
            "The Last Orb (Void Gas)|The last page of the periodic book; the final essence, a gas that shines like a solid star"
        };

        // ===================== Bileşikler =====================
        static readonly Dictionary<string, string[]> Compounds = new Dictionary<string, string[]>
        {
            { "water", new[] { "Water", "Hydrogen burns with oxygen to make water. The simplest potion of life." } },
            { "salt", new[] { "Table Salt", "Sodium and chlorine form an ionic bond. Salt keeps food safe by drying out germs." } },
            { "co2", new[] { "Carbon Dioxide", "Burning carbon makes CO2. It's the bubbles in fizzy drinks." } },
            { "ammonia", new[] { "Ammonia", "The Haber-Bosch process: nitrogen + hydrogen with heat and pressure. The world's fertiliser." } },
            { "saltpeter", new[] { "Saltpetre", "Potassium nitrate is a strong oxidiser - the heart of gunpowder and fireworks." } },
            { "bakingsoda", new[] { "Baking Soda", "Sodium bicarbonate is a base; it neutralises stomach acid." } },
            { "peroxide", new[] { "Hydrogen Peroxide", "One extra oxygen! It cleans wounds by oxidising germs. Breaks down when heated." } },
            { "bluevitriol", new[] { "Blue Vitriol", "Blue crystals of copper sulfate. Mixed with lime it becomes 'Bordeaux mixture' for vineyards." } },
            { "magnesia", new[] { "Magnesium Oxide", "Magnesium burns with a dazzling white flame. The flash of old-time photographers!" } },
            { "quicklime", new[] { "Quicklime", "Limestone heated very hot turns into CaO. It warms everything up when it meets water." } },
            { "coldpack", new[] { "Ammonium Nitrate", "NH4NO3 soaks up heat as it dissolves (endothermic). The secret of cold packs." } },
            { "lye", new[] { "Lye", "A strong base. It turns fat into soap (saponification)." } },
            { "limestone", new[] { "Limestone", "Chalk, marble, seashells... the base of mortar and buildings." } },
            { "rust", new[] { "Iron(III) Oxide", "Rust! The red ochre of cave paintings is the very same thing." } },
            { "ethanol", new[] { "Ethanol", "An alcohol that burns cleanly with a blue flame. Perfect lamp fuel." } },
            { "hcl", new[] { "Hydrochloric Acid", "It's in your stomach too! Dissolves rust and limescale." } },
            { "silica", new[] { "Silica (Glass)", "Sand melts at very high heat and becomes glass when it cools." } },
        };

        public static void ApplyCompounds(IEnumerable<CompoundData> all)
        {
            foreach (var c in all)
            {
                string[] t;
                if (Compounds.TryGetValue(c.Id, out t)) { c.NameEn = t[0]; c.FlavorEn = t[1]; }
            }
        }

        public static string Effect(PotionEffect e)
        {
            switch (e)
            {
                case PotionEffect.Hydrate: return "Hydrating";
                case PotionEffect.Preserve: return "Preserving";
                case PotionEffect.Fizz: return "Fizzing";
                case PotionEffect.Growth: return "Growth";
                case PotionEffect.Fireworks: return "Fireworks";
                case PotionEffect.Soothe: return "Soothing";
                case PotionEffect.Disinfect: return "Disinfecting";
                case PotionEffect.PestControl: return "Pest Control";
                case PotionEffect.Light: return "Light";
                case PotionEffect.Warmth: return "Warming";
                case PotionEffect.Cool: return "Cooling";
                case PotionEffect.Cleanse: return "Cleaning";
                case PotionEffect.Build: return "Building";
                case PotionEffect.Pigment: return "Paint";
                case PotionEffect.Fuel: return "Fuel";
                case PotionEffect.Dissolve: return "Dissolving";
                case PotionEffect.Glass: return "Glass";
                default: return "None";
            }
        }

        public static string Heat(HeatLevel h)
        {
            switch (h)
            {
                case HeatLevel.Cold: return "Cold";
                case HeatLevel.Warm: return "Warm";
                case HeatLevel.Hot: return "Hot";
                default: return "Inferno";
            }
        }

        // ===================== Ham maddeler =====================
        static readonly Dictionary<string, string[]> Raws = new Dictionary<string, string[]>
        {
            { "seawater", new[] { "Sea Water", "Village pier", "Electric current splits water into H2 and O2, and salt into Na and Cl." } },
            { "air", new[] { "Morning Air (in a jar)", "Hilltop", "Liquefy and distil air and you get nitrogen (78%) and oxygen (21%)." } },
            { "limestone", new[] { "Limestone", "Quarry", "Heated CaCO3 breaks down into CaO and CO2." } },
            { "ash", new[] { "Wood Ash", "The baker's oven", "'Potash' filtered from ash was once used to make soap." } },
            { "hematite", new[] { "Red Iron Ore", "Mine entrance", "Heat hematite (Fe2O3) with carbon and iron comes out." } },
            { "sulfur", new[] { "Yellow Sulfur Rock", "Sleeping volcano", "Pure sulfur crystals gather at volcano vents. Smells like rotten eggs!" } },
            { "malachite", new[] { "Malachite", "Green cave", "A green copper carbonate mineral. Smelt it to get shiny copper." } },
            { "sand", new[] { "Golden Sand", "Riverbank", "Most sand is silicon dioxide (SiO2)." } },
            { "magnesite", new[] { "White Magnesite", "White slopes", "Magnesium carbonate. Electrolysis gives light, shiny magnesium metal." } },
            { "guano", new[] { "Bat Guano", "Bat cave", "Once one of the most valuable sources of nitrate and phosphorus. Hold your nose!" } },
            { "dewcap", new[] { "Dewcap Mushroom", "Forest clearing", "Magic mushrooms that collect morning dew. Distil them for pure water elements." } },
            { "crystal", new[] { "Mystery Crystal", "Old stone circle", "Nobody knows where it came from. Every time it breaks, it whispers a different element." } },
        };

        public static void ApplyRaws(IEnumerable<RawMaterialData> all)
        {
            foreach (var r in all)
            {
                string[] t;
                if (Raws.TryGetValue(r.Id, out t)) { r.NameEn = t[0]; r.WhereEn = t[1]; r.FlavorEn = t[2]; }
            }
        }

        public static string Method(ExtractionMethod m)
        {
            switch (m)
            {
                case ExtractionMethod.Evaporation: return "Evaporation";
                case ExtractionMethod.Electrolysis: return "Electrolysis";
                case ExtractionMethod.Distillation: return "Distillation";
                case ExtractionMethod.Calcination: return "Calcination";
                case ExtractionMethod.Smelting: return "Smelting";
                case ExtractionMethod.Filtration: return "Filtration";
                default: return "Arcane Shattering";
            }
        }

        // ===================== Fail-Dex =====================
        static readonly Dictionary<string, string[]> Fails = new Dictionary<string, string[]>
        {
            { "mystery_goo", new[] { "Mystery Goo", "Something grey and suspicious is bubbling in the cauldron. Aqualf has filed it under 'art'.",
                "The atom ratios don't match any known compound. In chemistry, ratios (stoichiometry) are everything." } },
            { "near_miss", new[] { "So Close!", "The cauldron fizzed hopefully... then gave up.", "One atom too many or too few! Count the formula again." } },
            { "lukewarm_sludge", new[] { "Sleepy Sludge", "The ingredients looked at each other and yawned. Nothing happened.",
                "A reaction needs 'activation energy' to start. A bit more heat!" } },
            { "burnt", new[] { "Smells Burnt", "The lab filled with black smoke. Aqualf's eyebrows are a little thinner now.",
                "Too much heat can break the product down. Every reaction has an ideal temperature range." } },
            { "lumpy", new[] { "Lumpy Mess", "The potion looks like grandma's semolina pudding on a bad day.",
                "Stirring brings particles together; no collisions, no reaction (collision theory)." } },
            { "roof_blown", new[] { "Roof Blown Off!", "BOOM! The lab roof landed in the village square. The villagers clapped.",
                "Too much heat makes gases expand fast. Pressure in a closed pot = explosion." } },
            { "alkali_boom", new[] { "Hair on Fire", "An alkali metal met water. Aqualf's hair is now a torch.",
                "Sodium and potassium react violently with water, releasing hydrogen gas and heat." } },
            { "peroxide_foam", new[] { "Elephant Toothpaste", "A giant column of foam shot out of the cauldron and filled the lab!",
                "Heat makes hydrogen peroxide break into water and oxygen fast. The famous 'elephant toothpaste' experiment!" } },
            { "noble_snub", new[] { "Noble Silence", "The noble gases wouldn't talk to anyone. The cauldron just gave a smug glow.",
                "Noble gases have a full outer electron shell, so they hardly ever react." } },
            { "hydrogen_pop", new[] { "Pop!", "A teeny 'pop'. Aqualf counted it as a win.",
                "Hydrogen makes a typical 'pop' when it ignites with oxygen. It's the school test for H2." } },
            { "green_cloud", new[] { "Green Cloud", "A yellow-green cloud rose up. The lab cat is thinking about moving out.",
                "Chlorine gas is toxic and yellow-green. It's only safe in compounds like salt, not on its own." } },
            { "mercury_hat", new[] { "Silver Vapour", "Aqualf's pointy hat melted in a silver vapour. He feels a bit odd, too.",
                "Mercury vapour is very toxic. 'Mad as a hatter' comes from the mercury once used to make hats." } },
            { "glow_beard", new[] { "Glowing Beard", "Aqualf's beard glows green in the dark. Handy for reading at night.",
                "Radioactive elements give off radiation because their nuclei are unstable. (Never touch them in real life!)" } },
            { "midas", new[] { "Midas Cauldron", "For three seconds everything turned to gold. Then it turned back. Nobody believed it.",
                "The alchemists' dream of making gold: elements can't change into each other by chemistry, only by nuclear reactions." } },
            { "blinding_flash", new[] { "Blinding Flash", "A dazzling flash! Aqualf saw purple spots for ten minutes.",
                "Burning magnesium gives off very bright white light, but the wrong ratios just make a messy flare." } },
            { "rotten_egg", new[] { "Rotten Egg", "The whole village held its nose. The baker thought his bread had gone off.",
                "Heated sulfur compounds make hydrogen sulfide (H2S): the rotten egg smell." } },
            { "overflow", new[] { "Cauldron Overflow", "Too many ingredients! The cauldron rebelled and bubbled over.",
                "More stuff than needed means unwanted by-products." } },
            { "giant_pumpkins", new[] { "Pumpkin Invasion", "You gave fertiliser instead of bug spray. The next morning the house was buried under giant pumpkins.",
                "Nitrogen fertilisers speed up plant growth - not pest control!" } },
            { "frog_villager", new[] { "Ribbit!", "The villager drank the potion and... turned into a frog. Not too upset about it, though.",
                "This is pure magic. Nobody turns into a frog in chemistry. Probably." } },
            { "floating_villager", new[] { "Balloon Man", "After the fizzy potion the villager puffed up and floated at chimney height.",
                "CO2 gas takes up space as it escapes a liquid. It doesn't make people fly... but this is Sorchemist village." } },
            { "frozen_house", new[] { "Ice Palace", "The cooling potion worked too well. The whole house froze over.",
                "Endothermic reactions pull heat from their surroundings. Too much means ice." } },
            { "foam_flood", new[] { "Foam Flood", "The wrong thing instead of soap... the streets filled with foam. The kids are thrilled.",
                "Bases make soap and foam with fats." } },
            { "glowing_house", new[] { "Lantern House", "The house now glows day and night. The neighbours can't sleep.",
                "Light-making reactions (chemiluminescence) are real; fireflies do it too!" } },
            { "pink_village", new[] { "Pink Village", "The paint potion scattered in the wind. All the chickens are now reddish pink.",
                "Iron oxide is a natural pigment that has been used for thousands of years." } },
            { "fireworks_mishap", new[] { "Early Festival", "The firework potion went off in the wrong place. The village's best show ever... in the hay barn.",
                "Nitrates supply oxygen and speed up burning. That's why they are the base of gunpowder." } },
        };

        public static void ApplyFails(IEnumerable<FailEntry> all)
        {
            foreach (var f in all)
            {
                string[] t;
                if (Fails.TryGetValue(f.Id, out t)) { f.TitleEn = t[0]; f.DescriptionEn = t[1]; f.ScienceEn = t[2]; }
            }
        }

        public static string Rarity(FailRarity r)
        {
            switch (r)
            {
                case FailRarity.Common: return "Common";
                case FailRarity.Rare: return "Rare";
                case FailRarity.Epic: return "Epic";
                default: return "Legendary";
            }
        }

        // ===================== Köylüler =====================
        // id -> { ad, meslek, sohbet cümleleri... }
        // id -> { meslek (ad), unvan, bina adı, sohbet cümleleri... }
        static readonly Dictionary<string, string[]> Villagers = new Dictionary<string, string[]>
        {
            { "forge", new[] { "Blacksmith", "Anvil Master", "Blacksmith Forge", "Iron rusts, steel endures. The difference is carbon!", "Rhythm is everything at the anvil: clang, clang, clang!" } },
            { "glasshouse", new[] { "Botanist", "Greenhouse Sprout", "Glasshouse Conservatory", "Every plant needs a little water, a little light and lots of patience.", "The mushrooms in the glasshouse glow at night. Phosphorus, maybe?" } },
            { "watermill", new[] { "Miller", "Wheel Captain", "Arcane Watermill", "The river flows, the wheel turns, the water is purified. H2O, pure and clean!", "With every turn of the wheel a little magic mixes into the water." } },
            { "mine", new[] { "Miner", "Pickaxe Vein", "Gem Mine", "Salt, sulfur, quartz inside the rock... The earth is a treasure chest!", "If my candle goes out, I turn back. That's the rule." } },
            { "well", new[] { "Wellkeeper", "Essence Strainer", "Stone Essence Well", "Mana droplets rise from the bottom of the well. The pink ones are the sweetest.", "A bucket of water, a ladle of essence. All measured." } },
            { "workshop", new[] { "Chemist", "Vial Steam", "Potion & Steam Workshop", "Acid goes into water, never water into acid! Forget it and lose your eyebrows.", "My tubes are every colour. Each one is a different experiment." } },
            { "healer", new[] { "Herbalist", "Healing Leaf", "Herbalist & Healer Hut", "A salve for every wound, a herb for every ailment.", "Keep wounds clean and half of them heal themselves." } },
            { "crystal", new[] { "Lens Cutter", "Prism Eye", "Crystal Cutter & Lens House", "Sand melts in fire into glass; glass, cut with skill, becomes a lens.", "Light splits into seven colours in a crystal. I never tire of it!" } },
            { "library", new[] { "Librarian", "Rune Keeper", "Ancient Rune Library", "The Periodic Table is a book; every row is a family.", "If the runes glow, someone has opened a forbidden page." } },
            { "market", new[] { "Merchant", "Golden Scales", "Alchemy Trade Market", "Leather, cloth, shoes, crystals... Whatever you seek, it's at the market!", "My scales are honest, and so am I. Mostly." } },
            { "windmill", new[] { "Windkeeper", "Weather Reader", "Wind & Weather Mill", "When the wind blows we grind flour; when it doesn't, we read the weather.", "I feel the nitrogen, oxygen and argon in the air on my sails." } },
            { "observatory", new[] { "Astronomer", "Star Seeker", "Celestial Observatory", "Helium was found in the Sun before it was found on Earth. The sky is a laboratory!", "My telescope says there's a storm coming tomorrow." } },
            { "portal", new[] { "Portal Warden", "Rift Guardian", "Arcane Portal Gate", "Heavy, radioactive ores come through the gate. Be careful.", "Stars swirl inside the vortex. Don't stare, you'll get dizzy." } },
            { "dock", new[] { "Sky Captain", "Cloud Pilot", "Airship Dock & Port", "My airship flies on helium; hydrogen and fire are no joke!", "Distant kingdoms are waiting for your potions." } },
            { "hollow", new[] { "Innkeeper", "Root-Home Chef", "Hollow Tree Dwelling", "Hot tea, fresh bread, a cosy bed. Welcome to the Hollow Tree!", "A kitchen is a laboratory too: salt, baking soda, vinegar... all chemistry." } },
            { "stable", new[] { "Groom", "Ranch Caretaker", "Magical Creature Stable", "The mounts love glowing hay, and their manure smells of ammonia!", "My animals listen to me. The goats don't." } },
            { "distillery", new[] { "Distiller", "Copper Master", "Copper Distillery Plant", "Boil, evaporate, cool, collect drop by drop. That's distillation.", "My copper pipes are hot; don't touch, it leaves a mark." } },
            { "smeltery", new[] { "Smelter", "Lava Lord", "Volcanic Magma Smeltery", "Tungsten melts at 3422 degrees. My lava is hotter!", "Coal, wood, lava... My furnace never goes out." } },
            { "chronos", new[] { "Timekeeper", "Hourglass Savant", "Chronos Hourglass Tower", "Time is an element too; it just has no place on the table.", "While the sand flows, reactions speed up. Good for the impatient." } },
            { "sporedome", new[] { "Mycologist", "Spore Glow", "Bioluminescent Spore Dome", "My mushrooms glow in the dark: bioluminescence!", "Spores fly everywhere. Sorry if you sneeze." } },
            { "vault", new[] { "Vault Guardian", "Rune Lock", "Runic Treasure Vault", "There are Philosopher's Stone shards in the vault. I hold the key.", "Dangerous elements stay under lock. That's the rule." } },
            { "crater", new[] { "Meteor Hunter", "Starfall Seeker", "Starfall Meteorite Crater", "This meteorite came from space; it holds traces not found on Earth.", "My hammer, my magnifier and my patience. The crater awaits." } },
            { "aviary", new[] { "Courier", "Wind Wing", "Messenger Aviary Tower", "My birds bring new troubles every morning. And feathers.", "You've got mail, Aqualf! Oh, wrong address..." } },
        };

        public static void ApplyVillagers(IEnumerable<VillagerData> all)
        {
            foreach (var v in all)
            {
                string[] t;
                if (!Villagers.TryGetValue(v.Id, out t)) continue;
                v.NameEn = t[0]; v.RoleEn = t[1]; v.BuildingEn = t[2];
                if (t.Length > 3) { var a = new string[t.Length - 3]; System.Array.Copy(t, 3, a, 0, a.Length); v.AmbientEn = a; }
            }
        }

        // ===================== Görevler =====================
        // id -> { başarı cümlesi, diyalog satırları... }
        static readonly Dictionary<string, string[]> Quests = new Dictionary<string, string[]>
        {
            { "q_thirst", new[] { "My seedlings drank their fill! I saved my prettiest flower for you.",
                "Aqualf! The well has dried up and my seedlings are drooping.",
                "Could you make me some pure water? H's and O's, you know the drill..." } },
            { "q_pickles", new[] { "My pickles are rock solid! A fresh loaf from the oven is yours.",
                "The pickles I made for winter are starting to go mouldy, oh dear!",
                "I need that white, salty stuff that keeps food fresh. They say it comes from the sea." } },
            { "q_tummy", new[] { "My tummy's better! Hooray! I caught you a frog. Just kidding.",
                "I ate all of the Farmer's sour plums... my tummy is burning.",
                "Mum says 'you need a base to neutralise the acid'. Whatever that means!" } },
            { "q_bugs", new[] { "The bugs ran away! This year the vineyard will be full of grapes.",
                "My boy, mould and bugs have taken over my vineyard.",
                "My grandpa's grandpa made medicine with a blue stone. But don't bring fertiliser - the bugs would throw a party!" } },
            { "q_burn", new[] { "Ahh... ice cold. I'll forge you a new cauldron leg.",
                "I burned my hand at the forge. It's throbbing.",
                "Can you make something that soaks up heat and cools it down? But don't freeze my house!" } },
            { "q_wound", new[] { "The wounds are spotless. The village should be grateful for an alchemist like you.",
                "One of the woodcutters cut his arm. I need to clean the wound.",
                "You know that foaming water with an extra oxygen? Keep it cold, though, or it'll boil over!" } },
            { "q_dark", new[] { "The night is bright as day! The wolves won't come near now.",
                "My lantern glass broke. I'm guarding the village in the dark at night.",
                "I need a dazzling white light. I've heard about the stones on the white slopes." } },
            { "q_laundry", new[] { "The sheets are snow white! And the suds are just right.",
                "The festival tablecloths are black with soot.",
                "I need a strong base that cuts through grease and makes soap." } },
            { "q_mill", new[] { "The mill is solid as a rock. A sack of flour for you!",
                "The mill wall has cracked and wobbles a little more with every gust.",
                "Could you make that white stone they put in mortar to hold the stones together?" } },
            { "q_cold", new[] { "The square is toasty warm! The village council made you an honorary member.",
                "Winter came early and the cauldrons in the square won't warm up.",
                "Could you make that lime that gives off heat when it meets water? They say it needs very, very high heat." } },
            { "q_festival", new[] { "The sky is full of colours! People will talk about this festival for years.",
                "The harvest festival is coming and we have no fireworks!",
                "Potassium, nitrogen, oxygen... good old saltpetre! But don't test it near the barn, okay?" } },
            { "q_paint", new[] { "I painted the barn red! I didn't touch the chickens. Well, maybe a little.",
                "I want to paint the barn but there's no paint.",
                "Is it true that iron turns red when it rusts?" } },
            { "q_lamp", new[] { "The lamp burns with a blue flame and no soot at all! Wonderful.",
                "The oil in my lamps burns really sooty.",
                "I need a clean fuel with a blue flame. They say carbon, hydrogen and a bit of oxygen." } },
            { "q_window", new[] { "My shop window sparkles! Everyone can see my buns.",
                "My shop window broke and the wind is cooling my buns.",
                "They say glass is made from sand. But it needs a very hot fire!" } },
            { "q_well", new[] { "The buckets are full again! The village's water is in your hands.",
                "Aqualf, I can see the bottom of the well! My buckets are empty.",
                "Could you make a bucket of pure water? Two hydrogen, one oxygen... that's what you said." } },
            { "q_hide", new[] { "The hides don't smell or rot. Wonderful!",
                "The hides I just tanned are starting to spoil.",
                "They say salt protects leather. I need some salt." } },
            { "q_bread", new[] { "The dough rose and the soup is fizzing! The kitchen is a party.",
                "My bread dough won't rise and my soda has gone flat!",
                "Can you bring that gas that makes bubbles? Carbon and oxygen, plus a bit of heat." } },
            { "q_flowers", new[] { "The flowers are blooming and the bees are buzzing! A jar of honey for you.",
                "My bees' flowers have wilted and there's less honey.",
                "I need a fertiliser to feed the soil. Something with nitrogen..." } },
            { "q_wool", new[] { "The wool is snow white! My loom is working again.",
                "The wool is greasy and dirty; it won't spin into yarn.",
                "Could you make that strong base that strips away grease?" } },
            { "q_razor", new[] { "The razors are spotless! My customers can relax now.",
                "I need to clean the germs off my razors.",
                "That foaming liquid that kills germs... but don't heat it!" } },
            { "q_marble", new[] { "The stones shine and the rust marks are gone!",
                "My statue is covered in rust and limescale stains.",
                "I need an acid to dissolve the stains. Hydrogen and chlorine, so they say." } },
            { "q_ember", new[] { "Ahh... my hand feels cool. Thank you, alchemist!",
                "An ember jumped out of the charcoal pit and burned my hand!",
                "Could you make a cool compress that soaks up the heat?" } },
            { "q_moths", new[] { "The moths are gone! My fabrics are saved.",
                "Moths are eating holes in all my fabric!",
                "Could you make that blue crystal that drives bugs away?" } },
            { "q_night", new[] { "The river is bright as day! The fish came to the light.",
                "I'm going night fishing but my lantern went out.",
                "I need a dazzling white light." } },
            { "q_beams", new[] { "The roof beams sit solid as stone!",
                "My workshop's foundation cracked and the beams are wobbling.",
                "Can you make that white stone they add to mortar?" } },
            { "q_campfire", new[] { "The campfire burns soot-free with a blue flame!",
                "I'm camping in the forest tonight; I need a clean-burning fuel.",
                "Carbon, hydrogen and a little oxygen... that blue-flamed liquid!" } },
            { "q_leather", new[] { "The boots are bright red! Everyone will wear these at the festival.",
                "I need to dye the festival boots but I've run out of dye.",
                "The red of rust is exactly the colour I want!" } },
            { "q_glaze", new[] { "My pots are glazed like glass!",
                "I need glass powder to glaze my pots.",
                "Sand turns into glass, but only in a very hot fire!" } },
            // ---- Zekâ görevleri ----
            { "q_ice_pier", new[] { "The pier isn't slippery anymore! Salt lowered the freezing point of the ice and it melted.",
                "A hard frost came last night and the pier froze like glass. I nearly flew into the river!",
                "Sea water doesn't freeze easily, does it? I wonder why... Is it those little white grains in it?" } },
            { "q_bee_sting", new[] { "The swelling's down and the itch is gone! The base neutralised the acid in the sting.",
                "A bee stung me right on the tip of my nose. Now my nose is a turnip!",
                "They say a bee sting is a bit acidic. What if I put on that white powder that calms acid - the one that's good for tummies?" } },
            { "q_sour_bread", new[] { "The bread rose and it's not sour anymore. Baking soda + acid = bubbles!",
                "My sourdough got too wild and the loaves are sour as lemons.",
                "I need something to take away the sourness and make the dough rise too. Two birds with one stone!" } },
            { "q_axe_rust", new[] { "The axe is shiny! The acid dissolved the rust and the iron shows again.",
                "I left my axe out in the rain and it's covered in red rust.",
                "If only I could dissolve the rust instead of scraping it... A strong acid dissolves rust, right?" } },
            { "q_etching", new[] { "The pattern on the sword came out amazing! The acid only ate the spots without wax.",
                "The king's guard wants a dragon pattern on his sword. A hammer can't do that.",
                "I'll cover the steel in wax and scratch the pattern in. I need an acid to eat away the bare parts." } },
            { "q_sour_soil", new[] { "The cabbages are bursting out! The limestone neutralised the acid in the soil.",
                "My field's soil is sour and my cabbages are all yellow.",
                "My father used to sprinkle white stone powder on the soil. It's for building, but they say it sweetens the soil too!" } },
            { "q_damp_flour", new[] { "The barn is bone dry! The quicklime grabbed the moisture and locked it away.",
                "The barn is damp and the flour sacks are starting to go mouldy.",
                "That lime that pulls water out of the air and heats up when it gets wet... What if we put a bowl of it in the corner?" } },
            { "q_woodworm", new[] { "The worms left the wood alone! Copper salts protect timber.",
                "Woodworms are eating my cupboards. I hear crunching at night!",
                "Vine growers use a blue stone against bugs. Would the same thing protect wood?" } },
            { "q_raddle", new[] { "My rams have red chests! Now everyone knows whose sheep is whose.",
                "My sheep got mixed up with the neighbour's. They're all the same white!",
                "Shepherds used to mark sheep with red earth. Do you have that red paint that comes from iron?" } },
            { "q_fever", new[] { "The fever's down. The cooling compress drew the heat away.",
                "The Miller's child is burning up with fever.",
                "Can you make a compress from that salt that turns icy cold all by itself in water?" } },
            { "q_greenhouse", new[] { "The greenhouse is ready! Glass lets the light in and keeps the warmth; the seedlings will love it.",
                "I need to protect my rare seedlings before winter. I want to build a greenhouse.",
                "Something that lets light through but keeps the cold out... That clear stuff made from sand!" } },
            { "q_flare", new[] { "The sky lit up bright red and the lost hunter found his way!",
                "A hunter got lost in the forest and night is falling.",
                "If I shoot a flare into the sky he'll see it. Potassium, nitrogen, oxygen... that fast-burning salt!" } },
            { "q_lemonade", new[] { "The lemonade is fizzing! The guests are clinking their glasses.",
                "The lemonade at the inn is as flat as water. The guests are yawning.",
                "Can we put those tickly bubbles in it? Isn't that the gas that comes out when charcoal burns?" } },
            { "q_bleach", new[] { "The wedding dress is snow white! The peroxide oxidised the yellow stains.",
                "The Innkeeper's daughter is getting married, but the dress has turned yellow in the chest.",
                "They say that foaming germ-killing liquid also bleaches. Keep it cold, though!" } },
            { "q_boulder", new[] { "BOOM! The rock is in pieces and the road is open. Saltpetre is the heart of gunpowder.",
                "There was a landslide and a giant boulder blocked the road to the quarry.",
                "Forty men couldn't budge it. I need the salt inside gunpowder: saltpetre!" } },
            { "q_dehair", new[] { "The hair came off and the hide is clean! The strong base did its job.",
                "The hides are all hairy and I can't tan them.",
                "I need a strong base to loosen the hair and strip the fat. The stuff used to make soap!" } },
            { "q_smelly_boots", new[] { "The smell is gone! Baking soda neutralised the acids in the sweat.",
                "The Woodcutter brought his boots for repair but... I can't go into my own shop because of the smell!",
                "They say the smell comes from a sour acid. What was that white powder that neutralises acid?" } },
            { "q_mirror", new[] { "The full-length mirror is here! The customers adore themselves.",
                "My customers can't see themselves when they try on clothes.",
                "I need that clear material that becomes a mirror when you back it with silver. Sand and a big fire!" } },
            { "q_mordant", new[] { "The colours don't run even after washing! Copper sulfate locked the dye into the thread.",
                "The dye on my rugs runs off on the very first wash.",
                "Dyers fix the colour to the thread with a salt called a 'mordant'. Could it be that blue crystal?" } },
            { "q_red_glaze", new[] { "Red-glazed pots came out of the kiln! Iron oxide gives colour in fire.",
                "Everyone at the market wants red pots, but mine always come out brown.",
                "Time to use the red of iron rust. It's a colour that doesn't fade in fire!" } },
            { "q_smolder", new[] { "The fire is out! CO2 is heavier than air; it cut off the oxygen and smothered the flames.",
                "My charcoal pile is smouldering inside. If I pour water on it, it'll blow up in steam!",
                "I need a heavy gas that takes the place of oxygen and takes the fire's breath away." } },
            { "q_green_well", new[] { "The well is clear again! Peroxide burned the algae and left only water and oxygen.",
                "The well water has turned green with algae. Nobody wants to drink it.",
                "If only there were something that cleans water but leaves nothing but water and oxygen when it breaks down..." } },
            { "q_copper_pots", new[] { "The copper pots shine like the sun! The acid dissolved the tarnish.",
                "My copper pots have gone black with green stains.",
                "A bit of acid dissolves the dirty layer on copper. The same acid that's in your stomach!" } },
            { "q_shave_soap", new[] { "Plenty of lather and a smooth razor! The wonder of saponification.",
                "I've run out of shaving soap and my customers are getting dry shaves. Ouch!",
                "Bring me the strong base that turns fat into foamy soap and I'll cook up some soap right away." } },
        };

        public static void ApplyQuests(IEnumerable<QuestData> all)
        {
            foreach (var q in all)
            {
                string[] t;
                if (!Quests.TryGetValue(q.Id, out t)) continue;
                q.SuccessEn = t[0];
                var a = new string[t.Length - 1]; System.Array.Copy(t, 1, a, 0, a.Length);
                q.DialogueEn = a;
            }
        }

        // ===================== Pasifler =====================
        static readonly Dictionary<string, string[]> Passives = new Dictionary<string, string[]>
        {
            { "p_mendeleev", new[] { "Mendeleev's Notebook", "Element cards show atomic mass, period and group." } },
            { "p_purple", new[] { "Purple Flame", "The cauldron burns with a purple flame and crystals rain on successful potions." } },
            { "p_gold", new[] { "Gold Pouch", "Village gold income +25%." } },
            { "p_brew", new[] { "Patient Pot", "Potions brew 30% faster." } },
            { "p_owl", new[] { "Wise Owl", "Knowledge Point income +25%." } },
            { "p_column", new[] { "Distillation Column", "Breaking down gives 1 extra of every element." } },
            { "p_copper", new[] { "Copper Cauldron", "The cauldron holds 2 more atoms." } },
            { "p_luck", new[] { "Lucky Charm", "Better chance of a second element from the Mystery Box." } },
            { "p_builder", new[] { "Speedy Builder", "Building upgrades take 15% less time." } },
            { "p_sand", new[] { "Sands of Time", "The Mystery Box arrives every 5 hours instead of 6." } },
        };

        public static void ApplyPassives(IEnumerable<PassiveData> all)
        {
            foreach (var p in all)
            {
                string[] t;
                if (Passives.TryGetValue(p.Id, out t)) { p.NameEn = t[0]; p.DescriptionEn = t[1]; }
            }
        }
    }
}
