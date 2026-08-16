using System.Text.Json;

namespace DiplomskaNaloga.Models {
    public class DeckSpecial {
        private static readonly string CardsJsonPath = Path.Combine(AppContext.BaseDirectory, "Models", "cardsSpecial.json");
        private static readonly string imageTemplate = "/images/specialCards/{name}.png";

        private Random _random = new Random();

        private List<CardsSpecial> _cardsSpecial;

        public DeckSpecial() {
            _cardsSpecial = CreateFullDeck();
        }

        public List<CardsSpecial> DrawCards(int numberOfCards, bool testing) {
            List<CardsSpecial> drawnCards;
            int[] _testCardIds = { 0, 1 };

            if (testing) {
                drawnCards = _testCardIds
                    .Take(numberOfCards)
                    .Where(id => id < _cardsSpecial.Count)
                    .Select(ID => _cardsSpecial.First(c => c.ID == ID))
                    .ToList();
            } else {
                drawnCards = _cardsSpecial
                    .OrderBy(x => _random.Next())
                    .Take(numberOfCards)
                    .ToList();
            }

            _cardsSpecial.RemoveAll(card => drawnCards.Contains(card));

            return drawnCards;
        }

        public static List<CardsSpecial> CreateFullDeck() {
            var deck = new List<CardsSpecial>();

            var cardsJson = File.ReadAllText(CardsJsonPath);
            using var document = JsonDocument.Parse(cardsJson);
            var root = document.RootElement;

            int ID = 0;

            foreach (var card in root.EnumerateArray()) {
                var name = card.GetProperty("Name").GetString();
                var cost = card.GetProperty("Cost").GetString();
                var description = card.GetProperty("Description").GetString();

                CardEffect? effect = null;
                if (card.TryGetProperty("Effect", out var effectElement)) {
                    effect = new CardEffect {
                        Hand = effectElement.TryGetProperty("hand", out var handElem) ? handElem.GetString() : null,
                        Suit = effectElement.TryGetProperty("suit", out var suitElem) ? suitElem.GetString() : null,
                        Rank = effectElement.TryGetProperty("rank", out var rankElem) ? rankElem.GetString() : null,
                        Stat = effectElement.TryGetProperty("stat", out var statElem) ? statElem.GetString() : null,
                        Math = effectElement.TryGetProperty("math", out var mathElem) ? mathElem.GetString() : null,
                        Value = effectElement.TryGetProperty("value", out var valueElem) ? valueElem.GetInt32() : 0,
                    };
                }

                var specialCard = new CardsSpecial {
                    ID = ID++,
                    Name = name ?? string.Empty,
                    Cost = int.TryParse(cost, out var costValue) ? costValue : 0,
                    Description = description,
                    Effect = effect,
                    ImagePath = imageTemplate?.Replace("{name}", name.ToLower()) ?? string.Empty
                };

                deck.Add(specialCard);
            }

            return deck;
        }
    }
}