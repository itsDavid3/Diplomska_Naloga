using DiplomskaNaloga.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace DiplomskaNaloga.Controllers {
    public class GameController : Controller {
        private const string SessionGameKey = "Game";

        public IActionResult Index() {
            GameViewModel? model = null;
            var gameJSON = HttpContext.Session.GetString(SessionGameKey);

            if (string.IsNullOrEmpty(gameJSON)) {
                model = DefaultModel();
                SaveGame(model);
            } else {
                model = JsonSerializer.Deserialize<GameViewModel>(gameJSON) ?? new GameViewModel();
            }

            ApplySpecialCardEffects();
            model = LoadGame();
            SortHand(model);
            return View(model);
        }

        public IActionResult Shop() {
            var model = LoadGame();

            if (model == null || !model.InShop) {
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public IActionResult GameOver() {
            var model = LoadGame();

            if (model == null) {
                return RedirectToAction(nameof(Index));
            }

            model.hands = 0;

            SaveGame(model);
            return View(model);
        }



        public GameViewModel DefaultModel() {
            var deck = new Deck();
            var deckSpecial = new DeckSpecial();

            return new GameViewModel {
                Hand = deck.DrawCards(8, false),
                SelectedHand = new List<Card>(),
                SpecialCards = deckSpecial.DrawCards(2, false),
                BoughtSpecialCards = new List<CardsSpecial>(),
                pokerHand = "",
                chips = 0,
                mult = 0,
                ante = 1,
                round = 1,
                hands = 4,
                discards = 4,
                money = 0,
                roundScore = 0,
                InShop = false
            };
        }

        private void SaveGame(GameViewModel model) {
            HttpContext.Session.SetString(SessionGameKey, JsonSerializer.Serialize(model));
        }
        private GameViewModel? LoadGame() {
            var json = HttpContext.Session.GetString(SessionGameKey);
            return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<GameViewModel>(json);
        }

        [HttpGet]
        public IActionResult ExportGame() {
            var model = LoadGame();

            if (model == null) {
                return View("SaveLoad", new SaveLoadViewModel { ExportData = "" });
            }

            var json = JsonSerializer.Serialize(model);
            var exportString = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));

            return View("SaveLoad", new SaveLoadViewModel { ExportData = exportString });
        }

        [HttpPost]
        public IActionResult ImportGame(string importData) {
            try {
                if (string.IsNullOrWhiteSpace(importData)) {
                    var model = LoadGame();
                    return View("SaveLoad", new SaveLoadViewModel { ExportData = " " });
                }

                var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(importData.Trim()));
                var loadedModel = JsonSerializer.Deserialize<GameViewModel>(json);

                if (loadedModel != null) {
                    SaveGame(loadedModel);
                    return RedirectToAction(nameof(Index));
                }
            } catch (Exception ex) {
                var model = LoadGame();
                return View("SaveLoad", new SaveLoadViewModel { ExportData = " " });
            }

            return RedirectToAction(nameof(Index));
        }




        [HttpPost]
        public IActionResult NewHand() {
            HttpContext.Session.Remove(SessionGameKey);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult SelectCard(int id) {
            var model = LoadGame();

            if (model == null) {
                return RedirectToAction(nameof(Index));
            }

            var handCard = model.Hand.FirstOrDefault(c => c.ID == id);

            if (handCard != null && model.SelectedHand.Count < 5) {
                model.Hand.Remove(handCard);
                model.SelectedHand.Add(handCard);
            } else {
                var selectedCard = model.SelectedHand.FirstOrDefault(c => c.ID == id);

                if (selectedCard != null) {
                    model.SelectedHand.Remove(selectedCard);
                    model.Hand.Add(selectedCard);
                }
            }
            SaveGame(model);
            GetPokerHand();
            GetChips();
            SortHand(model);

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult DiscardCard() {
            var model = LoadGame();

            if (model == null) {
                return RedirectToAction(nameof(Index));
            }

            if (model.discards >= 1) {
                model.discards--;

                foreach (var card in model.SelectedHand) {
                    model.DiscardedCardIds.Add(card.ID);
                }

                var existingCardIds = model.Hand.Select(c => c.ID).ToHashSet();
                existingCardIds.UnionWith(model.DiscardedCardIds);
                existingCardIds.UnionWith(model.PlayedCardIds);

                var newCards = new List<Card>();
                var deck = new Deck();

                while (newCards.Count < model.SelectedHand.Count()) {
                    var drawnCards = deck.DrawCards(1, false);
                    var card = drawnCards[0];
                    if (!existingCardIds.Contains(card.ID)) {
                        newCards.Add(card);
                        existingCardIds.Add(card.ID);
                    }
                }

                model.Hand.AddRange(newCards);
                model.SelectedHand.Clear();
                ResetHandScore(model);

                SortHand(model);
                SaveGame(model);
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        public IActionResult BuySpecialCard(int id) {
            var model = LoadGame();

            if (model == null) {
                return RedirectToAction(nameof(Shop));
            }

            var specialCard = model.SpecialCards.FirstOrDefault(c => c.ID == id);

            if (specialCard != null && model.money >= specialCard.Cost && model.BoughtSpecialCards.Count < 5) {
                model.money -= specialCard.Cost;

                model.BoughtSpecialCards.Add(specialCard);
                model.SpecialCards.Remove(specialCard);
                specialCard.Cost /= 2;

                SaveGame(model);
            }

            return RedirectToAction(nameof(Shop));
        }

        [HttpPost]
        public IActionResult SellSpecialCard(int id) {
            var model = LoadGame();

            if (model == null) {
                return RedirectToAction(nameof(Shop));
            }

            var specialCard = model.BoughtSpecialCards.FirstOrDefault(c => c.ID == id);

            if (specialCard != null) {
                model.money += specialCard.Cost / 2;

                model.BoughtSpecialCards.Remove(specialCard);
                model.SpecialCards.Add(specialCard);
                specialCard.Cost *= 2;

                SaveGame(model);
            }

            return RedirectToAction(nameof(Shop));
        }



        [HttpPost]
        public IActionResult PlayHand() {
            GetScore();

            var model = LoadGame();

            if (model.SelectedHand == null)
                return RedirectToAction(nameof(Index));

            var roundGoal = Stake.CalculateRoundGoal(model.ante, model.round);

            if (model.roundScore >= roundGoal) {
                CompleteRound(model, roundGoal);
                SaveGame(model);

                return RedirectToAction(nameof(Shop));
            }

            if (model.hands == 1) {
                return RedirectToAction(nameof(GameOver));
            }

            PlaySelectedHand(model);
            SaveGame(model);

            return RedirectToAction(nameof(Index));
        }

        private void PlaySelectedHand(GameViewModel model) {
            foreach (var card in model.SelectedHand) {
                model.PlayedCardIds.Add(card.ID);
            }

            var cardsToDraw = model.SelectedHand.Count();

            model.hands--;

            var existingCardIds = model.Hand
                .Select(c => c.ID)
                .ToHashSet();

            existingCardIds.UnionWith(model.DiscardedCardIds);
            existingCardIds.UnionWith(model.PlayedCardIds);

            var deck = new Deck();
            var newCards = new List<Card>();

            while (newCards.Count < cardsToDraw) {
                var card = deck.DrawCards(1, false)[0];

                if (existingCardIds.Add(card.ID)) {
                    newCards.Add(card);
                }
            }

            model.Hand.AddRange(newCards);
            model.SelectedHand.Clear();

            ResetHandScore(model);
        }

        private void CompleteRound(GameViewModel model, double roundGoal) {
            model.SelectedHand.Clear();

            model.round++;
            model.money += 5;
            model.hands = 4;
            model.discards = 4;
            model.roundScore = 0;
            model.InShop = true;

            model.DiscardedCardIds.Clear();
            model.PlayedCardIds.Clear();

            ResetHandScore(model);

            model.Hand = new Deck().DrawCards(8, false);

            if (model.round % 3 == 1) {
                model.ante++;
            }

            var boughtCardIds = model.BoughtSpecialCards
                .Select(c => c.ID)
                .ToHashSet();

            model.SpecialCards = new DeckSpecial()
                .DrawCards(10, false)
                .Where(c => !boughtCardIds.Contains(c.ID))
                .Take(2)
                .ToList();
        }

        private static void ResetHandScore(GameViewModel model) {
            model.chips = 0;
            model.mult = 0;
            model.pokerHand = "";
        }



        [HttpPost]
        public IActionResult RerollShop() {
            var model = LoadGame();

            if (model.money >= 5) {
                var boughtCardIds = model.BoughtSpecialCards.Select(c => c.ID).ToHashSet();
                var deckSpecial = new DeckSpecial();
                var availableCards = deckSpecial.DrawCards(10, false)
                    .Where(c => !boughtCardIds.Contains(c.ID))
                    .Take(2)
                    .ToList();

                model.SpecialCards = availableCards;
                model.money -= 5;
            }

            SaveGame(model);
            return RedirectToAction(nameof(Shop));
        }

        [HttpPost]
        public IActionResult NextRound() {
            var model = LoadGame();

            model.InShop = false;

            SaveGame(model);
            return RedirectToAction(nameof(Index));
        }


        public string GetPokerHand() {
            var model = LoadGame();

            var cards = model.SelectedHand.Select(c => c.ID).ToArray();

            var ranks = cards.Select(id => id % 13).OrderBy(r => r).ToArray();
            var suits = cards.Select(id => id / 13).ToArray();

            int[]? groups = model.SelectedHand.Any() ? ranks.GroupBy(r => r).Select(g => g.Count()).OrderByDescending(c => c).ToArray() : null;

            int count = cards.Length;
            bool flush = count == 5 && suits.Distinct().Count() == 1;
            bool straight = count == 5 && IsStraight(ranks);

            if (count == 5 && straight && flush) {
                bool isRoyal = (ranks[0] == 8 && ranks[1] == 9 && ranks[2] == 10 && ranks[3] == 11 && ranks[4] == 12);

                if (isRoyal) {
                    model.pokerHand = "Kraljeva Lestvica";
                    model.mult = 8;
                } else {
                    model.pokerHand = "Barvna Lestvica";
                    model.mult = 7;
                }
            } else if (count == 5 && !straight && flush) {
                model.pokerHand = "Barva";
                model.mult = 4;
            } else if (count == 5 && straight && !flush) {
                model.pokerHand = "Lestvica";
                model.mult = 4;
            } else if (count == 5 && groups?.SequenceEqual(new[] { 3, 2 }) == true) {
                model.pokerHand = "Polna Hiša";
                model.mult = 5;
            } else if (groups?.SequenceEqual(new[] { 4 }) == true | groups?.SequenceEqual(new[] { 4, 1 }) == true) {
                model.pokerHand = "Štirica";
                model.mult = 6;
            } else if (groups?[0] == 3) {
                model.pokerHand = "Tris";
                model.mult = 3;
            } else if (groups?.SequenceEqual(new[] { 2, 2 }) == true | groups?.SequenceEqual(new[] { 2, 2, 1 }) == true) {
                model.pokerHand = "Dva Para";
                model.mult = 2;
            } else if (groups?[0] == 2) {
                model.pokerHand = "En Par";
                model.mult = 2;
            } else if (count > 0) {
                model.pokerHand = "Visoka Karta";
                model.mult = 1;
            } else {
                model.pokerHand = "";
                model.mult = 0;
            }

            int[] AceCards = { 12, 25, 38, 51 };
            int[] KingCards = { 11, 24, 37, 50 };

            foreach (var card in model.SelectedHand) {
                card.IsInPokerHand = false;
                if (AceCards.Contains(card.ID)) {
                    card.Value = 11;
                }
            }

            if ((model.pokerHand == "Lestvica" | model.pokerHand == "Barvna Lestvica" | model.pokerHand == "Kraljevska Barvna Lestvica") && cards.Any(AceCards.Contains) && !cards.Any(KingCards.Contains)) {
                foreach (var ace in model.SelectedHand.Where(c => AceCards.Contains(c.ID))) {
                    ace.Value = 1;
                }
            }

            SaveGame(model);

            return model.pokerHand;
        }

        private static bool IsStraight(int[] sortedRanks) {
            if (sortedRanks.Distinct().Count() != 5)
                return false;

            bool normal = true;
            for (int i = 1; i < 5; i++) {
                if (sortedRanks[i] != sortedRanks[i - 1] + 1) {
                    normal = false;
                    break;
                }
            }

            if (normal)
                return true;

            return sortedRanks.SequenceEqual(new[] { 0, 1, 2, 3, 12 });
        }

        private void ApplySpecialCardEffects() {
            var model = LoadGame();

            if (model.BoughtSpecialCards.Count == 0) {
                return;
            } else {
                foreach (var specialCard in model.BoughtSpecialCards) {
                    switch (TriggerCondition(specialCard.Effect)) {
                        case "Roka":
                            if (model.pokerHand == specialCard.Effect.Hand) {
                                ApplyStatModifier(specialCard.Effect, model);
                            }
                            break;
                        case "Barva":
                            if (model.SelectedHand.Any(card => card.Suit == specialCard.Effect.Suit)) {
                                ApplyStatModifier(specialCard.Effect, model);
                            }
                            break;
                        case "Številka":
                            if (model.SelectedHand.Any(card => IsRankMatch(card.Rank, specialCard.Effect.Rank))) {
                                ApplyStatModifier(specialCard.Effect, model);
                            }
                            break;
                        case "NoTrigger":
                            ApplyStatModifier(specialCard.Effect, model);
                            break;
                    }
                }
            }                
        }

        private string TriggerCondition(CardEffect effect) {
            if (!string.IsNullOrEmpty(effect.Hand))
                return "Roka";

            if (!string.IsNullOrEmpty(effect.Suit))
                return "Barva";

            if (!string.IsNullOrEmpty(effect.Rank))
                return "Številka";

            return "NoTrigger";
        }

        private void ApplyStatModifier(CardEffect effect, GameViewModel model) {
            switch (effect.Math) {
                case "seštej":
                    switch (effect.Stat) {
                        case "žetoni": 
                            model.chips += effect.Value;
                            break;
                        case "množ": 
                            model.mult += effect.Value;
                            break;
                        case "roke": 
                            model.hands += effect.Value;
                            break;
                        case "zavržki": 
                            model.discards += effect.Value;
                            break;
                    }
                    break;
                case "odštej":
                    switch (effect.Stat) {
                        case "žetoni": 
                            model.chips -= effect.Value;
                            break;
                        case "množ": 
                            model.mult -= effect.Value;
                            break;
                        case "roke": 
                            model.hands -= effect.Value;
                            break;
                        case "zavržki": 
                            model.discards -= effect.Value;
                            break;
                    }
                    break;
                case "pomnoži":
                    switch (effect.Stat) {
                        case "žetoni": 
                            model.chips *= effect.Value;
                            break;
                        case "množ": 
                            model.mult *= effect.Value;
                            break;
                        case "roke": 
                            model.hands *= effect.Value;
                            break;
                        case "zavržki": 
                            model.discards *= effect.Value;
                            break;
                    }
                    break;
            }
            SaveGame(model);
        }

        private bool IsRankMatch(string cardRank, string effectRank) {
            if (effectRank?.ToLower() == "face") {
                return cardRank is "Fant" or "Kraljica" or "Kralj";
            }

            return cardRank == effectRank;
        }

        public int GetChips() {
            var model = LoadGame();

            model.chips = 0;

            model.chips += model.SelectedHand.Count() * 5;

            foreach (var c in model.SelectedHand) {
                model.chips += c.Value;
            }

            SaveGame(model);
            return model.chips;
        }

        public int GetScore() {
            var model = LoadGame();

            model.roundScore += (model.chips * model.mult);

            SaveGame(model);
            return model.roundScore;
        }

        private void SortHand(GameViewModel model) {
            var rankOrder = new Dictionary<string, int>
            {
                { "As", 0 },
                { "Kralj", 1 },
                { "Kraljica", 2 },
                { "Fant", 3 },
                { "Desetka", 4 },
                { "Devetka", 5 },
                { "Osemka", 6 },
                { "Sedmica", 7 },
                { "Šestica", 8 },
                { "Petica", 9 },
                { "Štirica", 10 },
                { "Trojka", 11 },
                { "Dvojka", 12 }
            };

            model.Hand = model.Hand
                .OrderBy(c => rankOrder.TryGetValue(c.Rank, out var order) ? order : 13)
                .ThenBy(c => c.ID)
                .ToList();
        }
    }
}