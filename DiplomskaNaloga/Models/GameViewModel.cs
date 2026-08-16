namespace DiplomskaNaloga.Models {
    public class GameViewModel {
        public List<Card> Hand { get; set; } = new();
        public List<Card> SelectedHand { get; set; } = new();
        public List<CardsSpecial> SpecialCards { get; set; } = new();
        public List<CardsSpecial> BoughtSpecialCards { get; set; } = new();
        public List<int> DiscardedCardIds { get; set; } = new();
        public List<int> PlayedCardIds { get; set; } = new();
        public string? pokerHand { get; set; }
        public int chips { get; set; }
        public int mult { get; set; }
        public int ante { get; set; }
        public int round { get; set; }
        public int hands { get; set; }
        public int discards { get; set; }
        public int money { get; set; }
        public int roundScore { get; set; }
        public bool InShop { get; set; }
    }
}