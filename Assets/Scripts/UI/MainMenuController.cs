using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LightChasePrototype.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject instructionsPanel;
        [SerializeField] private string gameplaySceneName = "LightChasePrototype";

        private Action<string> _sceneLoader;
        private Action _quitAction;

        public bool InstructionsVisible => instructionsPanel != null && instructionsPanel.activeSelf;
        public string GameplaySceneName => gameplaySceneName;

        public void Configure(GameObject assignedInstructionsPanel, string assignedGameplaySceneName)
        {
            instructionsPanel = assignedInstructionsPanel;
            gameplaySceneName = assignedGameplaySceneName;
            HideInstructions();
        }

        public void ConfigureActionsForTests(Action<string> sceneLoader, Action quitAction)
        {
            _sceneLoader = sceneLoader;
            _quitAction = quitAction;
        }

        private void Awake()
        {
            HideInstructions();
        }

        private void Update()
        {
            if (InstructionsVisible && Input.GetKeyDown(KeyCode.Escape))
            {
                HideInstructions();
            }
        }

        public void PlayGame()
        {
            var sceneLoader = _sceneLoader ?? SceneManager.LoadScene;
            sceneLoader.Invoke(gameplaySceneName);
        }

        public void ShowInstructions()
        {
            if (instructionsPanel == null)
            {
                return;
            }

            instructionsPanel.SetActive(true);
        }

        public void HideInstructions()
        {
            if (instructionsPanel == null)
            {
                return;
            }

            instructionsPanel.SetActive(false);
        }

        public void QuitGame()
        {
            if (_quitAction != null)
            {
                _quitAction.Invoke();
                return;
            }

            Application.Quit();
        }
    }
}
