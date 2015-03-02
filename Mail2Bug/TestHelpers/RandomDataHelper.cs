using System;
using System.Collections.Generic;
using Microsoft.Test.Text;

namespace Mail2Bug.TestHelpers
{
    public class RandomDataHelper
    {
        static RandomDataHelper()
        {
            ConversationIdProperties.MinNumberOfCodePoints = 64;
            ConversationIdProperties.MaxNumberOfCodePoints = 255;
            ConversationIdProperties.UnicodeRanges.Clear();
            ConversationIdProperties.UnicodeRanges.Add(new UnicodeRange('A', 'F'));
            ConversationIdProperties.UnicodeRanges.Add(new UnicodeRange('0', '9'));

            NameProperties.MinNumberOfCodePoints = 5;
            NameProperties.MinNumberOfCodePoints = 15;
            Ranges.ForEach(x => NameProperties.UnicodeRanges.Add(x));

            AliasProperties.MinNumberOfCodePoints = 4;
            AliasProperties.MaxNumberOfCodePoints = 10;
            AliasProperties.UnicodeRanges.Clear();
            AliasProperties.UnicodeRanges.Add(new UnicodeRange(UnicodeChart.Latin));

            SubjectProperties.MinNumberOfCodePoints = 10;
            SubjectProperties.MaxNumberOfCodePoints = 50;
            Ranges.ForEach(x => SubjectProperties.UnicodeRanges.Add(x));

            BodyProperties.MinNumberOfCodePoints = 0;
            BodyProperties.MaxNumberOfCodePoints = 500;
            Ranges.ForEach(x => BodyProperties.UnicodeRanges.Add(x));

        }

        public static string GetConversationId(int seed)
        {
            return StringFactory.GenerateRandomString(ConversationIdProperties, seed);
        }

        public static string GetName(int seed)
        {
            return StringFactory.GenerateRandomString(NameProperties, seed);
        }

        public static string GetAlias(int seed)
        {
            return StringFactory.GenerateRandomString(AliasProperties, seed);
        }

        public static string GetSubject(int seed)
        {
            return StringFactory.GenerateRandomString(SubjectProperties, seed);
        }

        public static string GetBody(int seed)
        {
            return StringFactory.GenerateRandomString(BodyProperties, seed);
        }

        public static string GetRandomMessageSeparator(int seed)
        {
            var rand = new Random(seed);
            var separators = new[]
                                 {
                                     new String('_', rand.Next(4,50)), 
                                     "-----Original Message", 
                                     "From:"
                                 };

            return separators[rand.Next(0, separators.Length - 1)];
        }



        public static readonly List<UnicodeRange> Ranges = new List<UnicodeRange>
                {
                    new UnicodeRange(UnicodeChart.Latin),
                    new UnicodeRange(UnicodeChart.MiscellaneousMathematicalSymbolsA),
                    new UnicodeRange(UnicodeChart.MiscellaneousMathematicalSymbolsB),
                    new UnicodeRange(UnicodeChart.InvisibleOperators),
                    new UnicodeRange(UnicodeChart.JapaneseChess),
                    new UnicodeRange(UnicodeChart.Hebrew),
                };

        public static readonly StringProperties ConversationIdProperties = new StringProperties();
        public static readonly StringProperties NameProperties = new StringProperties();
        public static readonly StringProperties AliasProperties = new StringProperties();
        public static readonly StringProperties SubjectProperties = new StringProperties();
        public static readonly StringProperties BodyProperties = new StringProperties();
    }
}
