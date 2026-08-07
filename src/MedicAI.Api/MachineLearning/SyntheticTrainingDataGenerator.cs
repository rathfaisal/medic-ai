using System.Globalization;
using System.Text;

namespace MedicAI.Api.MachineLearning;

public static class SyntheticTrainingDataGenerator
{
    private static readonly string[] Symptoms =
    {
        "cough", "fever", "headache", "abdominal pain", "back pain", "dizziness",
        "vomiting", "sore throat", "skin rash", "minor injury", "urinary symptoms",
        "fatigue", "ear pain", "diarrhoea", "joint pain"
    };

    public static void EnsureDatasets(string trainingPath, int minimumRows, string validationTemplatePath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(trainingPath)!);

        if (!File.Exists(trainingPath) || File.ReadLines(trainingPath).Skip(1).Take(minimumRows).Count() < minimumRows)
        {
            GenerateTrainingData(trainingPath, minimumRows);
        }

        if (!File.Exists(validationTemplatePath))
        {
            GenerateDoctorValidationTemplate(validationTemplatePath, 120);
        }
    }

    private static void GenerateTrainingData(string path, int rows)
    {
        var random = new Random(4242002511.GetHashCode());
        var builder = new StringBuilder();
        builder.AppendLine("Label,Age,MainSymptom,DurationHours,PainSeverity,Fever,Vomiting,Dizziness,WorseningSymptoms,ExistingCondition,MissingInformation");

        for (var i = 0; i < rows; i++)
        {
            var age = random.Next(5, 91);
            var symptom = Symptoms[random.Next(Symptoms.Length)];
            var durationHours = RandomDuration(random);
            var pain = random.Next(0, 11);
            var fever = Chance(random, symptom is "fever" or "sore throat" or "cough" ? 0.65 : 0.18);
            var vomiting = Chance(random, symptom is "vomiting" or "abdominal pain" or "diarrhoea" ? 0.55 : 0.10);
            var dizziness = Chance(random, symptom is "dizziness" or "headache" or "fatigue" ? 0.50 : 0.12);
            var worsening = Chance(random, 0.32);
            var existingCondition = Chance(random, age >= 60 ? 0.52 : 0.22);
            var missingInformation = Chance(random, 0.12);

            var score = 0.0;
            score += pain * 0.42;
            score += worsening * 2.0;
            score += fever * 0.9;
            score += vomiting * 0.8;
            score += dizziness * 0.65;
            score += existingCondition * 1.25;
            score += age >= 65 ? 1.2 : age <= 10 ? 0.8 : 0;
            score += durationHours <= 8 ? 0.65 : durationHours >= 168 ? 0.4 : 0;
            score += SymptomWeight(symptom);
            score += (random.NextDouble() - 0.5) * 3.4; // intentional ambiguity and overlap
            score -= missingInformation * 0.25;

            var label = score switch
            {
                < 3.7 => "Routine",
                < 6.7 => "Priority",
                _ => "Urgent"
            };

            // Add a small amount of adjacent-class label uncertainty to avoid a perfectly deterministic generator.
            if (random.NextDouble() < 0.06)
            {
                label = label switch
                {
                    "Routine" => "Priority",
                    "Urgent" => "Priority",
                    _ => random.NextDouble() < 0.5 ? "Routine" : "Urgent"
                };
            }

            builder.AppendLine(string.Join(",",
                label,
                age.ToString(CultureInfo.InvariantCulture),
                Csv(symptom),
                durationHours.ToString(CultureInfo.InvariantCulture),
                pain.ToString(CultureInfo.InvariantCulture),
                fever.ToString(CultureInfo.InvariantCulture),
                vomiting.ToString(CultureInfo.InvariantCulture),
                dizziness.ToString(CultureInfo.InvariantCulture),
                worsening.ToString(CultureInfo.InvariantCulture),
                existingCondition.ToString(CultureInfo.InvariantCulture),
                missingInformation.ToString(CultureInfo.InvariantCulture)));
        }

        File.WriteAllText(path, builder.ToString());
    }

    private static void GenerateDoctorValidationTemplate(string path, int rows)
    {
        var random = new Random(2407);
        var builder = new StringBuilder();
        builder.AppendLine("ScenarioId,Age,MainSymptom,DurationHours,PainSeverity,Fever,Vomiting,Dizziness,WorseningSymptoms,ExistingCondition,MissingInformation,DoctorLabel,DoctorNotes");

        for (var i = 1; i <= rows; i++)
        {
            var symptom = Symptoms[random.Next(Symptoms.Length)];
            builder.AppendLine(string.Join(",",
                $"VAL-{i:000}",
                random.Next(5, 91),
                Csv(symptom),
                RandomDuration(random),
                random.Next(0, 11),
                Chance(random, 0.30),
                Chance(random, 0.20),
                Chance(random, 0.22),
                Chance(random, 0.35),
                Chance(random, 0.30),
                Chance(random, 0.15),
                string.Empty,
                string.Empty));
        }

        File.WriteAllText(path, builder.ToString());
    }

    private static int RandomDuration(Random random)
    {
        var bucket = random.NextDouble();
        if (bucket < 0.45) return random.Next(1, 49);
        if (bucket < 0.85) return random.Next(49, 337);
        return random.Next(337, 1441);
    }

    private static int Chance(Random random, double probability) => random.NextDouble() < probability ? 1 : 0;

    private static double SymptomWeight(string symptom) => symptom switch
    {
        "abdominal pain" => 0.8,
        "vomiting" => 0.7,
        "dizziness" => 0.65,
        "minor injury" => 0.55,
        "fever" => 0.45,
        "urinary symptoms" => 0.4,
        _ => 0.15
    };

    private static string Csv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";
}
