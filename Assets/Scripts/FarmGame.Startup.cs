using System;
using UnityEngine;

namespace EverythingGrows
{
    public sealed partial class FarmGame
    {
        internal PlantDefinition[] SetupDefinitions => definitions;
        public bool IsInitialized { get; private set; }
        public string InitializationError { get; private set; }
        private bool initializationStarted;

        public void Initialize(bool grantStartingItems = true)
        {
            if (IsInitialized || initializationStarted) return;
            var resources = GameSetupResources.Load();
            var report = new SetupValidationReport();
            SetupDataValidator.CheckResources(resources, report, grantStartingItems, GameSession.Enabled);
            FarmSetupValidator.Check(this, resources, report, GameSession.Enabled, Application.isPlaying && grantStartingItems);
            report.Publish();
            if (report.HasErrors)
            {
                InitializationError = report.FirstError;
                enabled = false;
                return;
            }
            initializationStarted = true;
            try
            {
                InitializeRuntime(grantStartingItems, resources);
                IsInitialized = true;
                InitializationError = null;
                enabled = true;
                if (Application.isPlaying && grantStartingItems) GameSession.Bind(this);
            }
            catch (Exception problem)
            {
                IsInitialized = false;
                InitializationError = "초기화 실패: " + gameObject.scene.path + " :: " + name + " — " + problem.Message + ". Console의 원인을 수정한 뒤 Play를 다시 시작하세요.";
                enabled = false;
                Debug.LogError(InitializationError, this);
                Debug.LogException(problem, this);
            }
        }
    }
}
