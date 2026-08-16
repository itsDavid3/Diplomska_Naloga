using System.Text.Json;

namespace DiplomskaNaloga.Models {
    public class Deck {
        private static readonly string CardsJsonPath = Path.Combine(AppContext.BaseDirectory, "Models", "cards.json");
        private static readonly string imageTemplate = "/images/cards/{rank}_{suit}.png";
        private Random _random = new Random();

        private List<Card> _cards;

        public Deck() {
            _cards = CreateFullDeck();
        }

        public List<Card> DrawCards(int numberOfCards, bool testing) {
            List<Card> drawnCards;
            int[] _testCardIds = { 12, 11, 10, 9, 8 };

            if (testing) {
                drawnCards = _testCardIds
                    .Take(numberOfCards)
                    .Select(ID => _cards.First(c => c.ID == ID))
                    .ToList();
            } else {
                drawnCards = _cards
                    .OrderBy(x => _random.Next())
                    .Take(numberOfCards)
                    .ToList();
            }

            _cards.RemoveAll(card => drawnCards.Contains(card));

            return drawnCards;
        }

        public static List<Card> CreateFullDeck() {
            var deck = new List<Card>();


            var cardsJson = File.ReadAllText(CardsJsonPath);
            using var document = JsonDocument.Parse(cardsJson);
            var root = document.RootElement;

            var ranks = root.GetProperty("ranks").EnumerateArray().ToList();
            var suits = root.GetProperty("suits").EnumerateArray().ToList();

            int ID = 0;


            foreach (var suit in suits) {
                var suitName = suit.GetProperty("name").GetString();

                foreach (var rank in ranks) {
                    var rankName = rank.GetProperty("name").GetString();
                    var rankValues = rank.GetProperty("value").GetInt32();

                    var card = new Card {
                        ID = ID++,
                        Rank = rankName,
                        Suit = suitName,
                        Value = rankValues,
                        ImagePath = imageTemplate?.Replace("{rank}", rankName.ToLower())
                            .Replace("{suit}", suitName.ToLower()) ?? string.Empty
                    };

                    deck.Add(card);
                }
            }

            return deck;
        }
    }
}