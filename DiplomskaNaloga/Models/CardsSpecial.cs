namespace DiplomskaNaloga.Models {
    public class CardsSpecial {
        public int ID { get; set; }
        public string? Name { get; set; }
        public int Cost { get; set; }
        public string? Description { get; set; }
        public required CardEffect Effect { get; set; }
        public string? ImagePath { get; set; }
    }

    public class CardEffect {
        public string? Hand { get; set; }  // Pair, Straight ...
        public string? Suit { get; set; }  // Diamonds, Hearts, Spades, Clubs
        public string? Rank { get; set; }  // 2, 3, 4, Face, Ace ...
        public string? Stat { get; set; }  // Mult, Chips, Hands, Discards
        public string? Math { get; set; } // add, sub, mult ...
        public int Value { get; set; }  // 4, 6, 8 ... 
    }
}