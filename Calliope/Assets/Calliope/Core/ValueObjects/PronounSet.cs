namespace Calliope.Core.ValueObjects
{
    /// <summary>
    /// Pronoun set for text variable substitutions;
    /// Enables "{speaker.pronounce.subject} said..." to "he/she/they/etc. said..."
    /// </summary>
    [System.Serializable]
    public struct PronounSet
    {
        public string Subject;          // "He", "She", "They"
        public string Object;           // "Him", "Her", "Them"
        public string Possessive;       // "His", "Her", "Their"
        public string Reflexive;        // "Himself", "Herself", "Themselves"

        public PronounSet(string subject, string obj, string possessive, string reflexive)
        {
            Subject = subject;
            Object = obj;
            Possessive = possessive;
            Reflexive = reflexive;
        }
        
        // Convenience Presets
        public static PronounSet HeHim => new PronounSet("he", "him", "his", "himself");
        public static PronounSet SheHer => new PronounSet("she", "her", "her", "herself");
        public static PronounSet TheyThem => new PronounSet("they", "them", "their", "themselves");
    }
}