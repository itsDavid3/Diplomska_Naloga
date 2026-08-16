namespace DiplomskaNaloga.Models {
    public class Card {
        public int ID { get; set; }
        public string? Rank { get; set; }
        public string? Suit { get; set; }
        public int Value { get; set; }
        public string? ImagePath { get; set; }
        public bool IsInPokerHand { get; set; } = false;
    }
}