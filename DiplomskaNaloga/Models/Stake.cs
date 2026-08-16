namespace DiplomskaNaloga.Models {
    public class Stake {
        public static int CalculateBaseGoal(int ante) {
            if (ante <= 1) return 300;

            int previousGoal = CalculateBaseGoal(ante - 1);
            return (previousGoal * 3) + 100;
        }

        public static double CalculateRoundGoal(int ante, int round) {
            int baseGoal = CalculateBaseGoal(ante);

            int roundInAnte = ((round - 1) % 3) + 1;

            double multiplier = roundInAnte switch {
                1 => 1.0,
                2 => 1.5,
                3 => 2.0,
                _ => 1.0
            };

            return baseGoal * multiplier;
        }
    }
}