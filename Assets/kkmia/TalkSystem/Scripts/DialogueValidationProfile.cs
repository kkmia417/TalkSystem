using System.Collections.Generic;
using UnityEngine;

namespace kkmia.TalkSystem
{
    [CreateAssetMenu(menuName = "kkmia/Talk System/Dialogue Validation Profile")]
    public sealed class DialogueValidationProfile : ScriptableObject
    {
        [SerializeField] private List<TextAsset> csvFiles = new List<TextAsset>();
        [SerializeField] private CharacterExpressionDatabase characterDatabase;
        [SerializeField] private BackgroundDatabase backgroundDatabase;
        [SerializeField] private AudioDatabase audioDatabase;
        [SerializeField] private DialogueKeyCatalog eventKeyCatalog;
        [SerializeField] private DialogueKeyCatalog conditionKeyCatalog;
        [SerializeField] private DialogueKeyCatalog variableCatalog;
        [SerializeField] private DialogueValidationSeverity missingReferenceSeverity = DialogueValidationSeverity.Warning;
        [SerializeField] private bool runAsBuildGate = true;
        [SerializeField] private bool failBuildOnErrors = true;

        public IReadOnlyList<TextAsset> CsvFiles
        {
            get { return csvFiles; }
        }

        public CharacterExpressionDatabase CharacterDatabase
        {
            get { return characterDatabase; }
            set { characterDatabase = value; }
        }

        public BackgroundDatabase BackgroundDatabase
        {
            get { return backgroundDatabase; }
            set { backgroundDatabase = value; }
        }

        public AudioDatabase AudioDatabase
        {
            get { return audioDatabase; }
            set { audioDatabase = value; }
        }

        public DialogueKeyCatalog EventKeyCatalog
        {
            get { return eventKeyCatalog; }
            set { eventKeyCatalog = value; }
        }

        public DialogueKeyCatalog ConditionKeyCatalog
        {
            get { return conditionKeyCatalog; }
            set { conditionKeyCatalog = value; }
        }

        public DialogueKeyCatalog VariableCatalog
        {
            get { return variableCatalog; }
            set { variableCatalog = value; }
        }

        public DialogueValidationSeverity MissingReferenceSeverity
        {
            get { return missingReferenceSeverity; }
            set { missingReferenceSeverity = value; }
        }

        public bool RunAsBuildGate
        {
            get { return runAsBuildGate; }
            set { runAsBuildGate = value; }
        }

        public bool FailBuildOnErrors
        {
            get { return failBuildOnErrors; }
            set { failBuildOnErrors = value; }
        }

        public void SetCsvFiles(IEnumerable<TextAsset> files)
        {
            csvFiles.Clear();
            if (files == null) return;

            foreach (var file in files)
            {
                if (file != null)
                    csvFiles.Add(file);
            }
        }
    }
}
