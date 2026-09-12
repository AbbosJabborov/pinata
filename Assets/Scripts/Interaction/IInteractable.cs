using System.Collections.Generic;

namespace Pinata.Interaction
{
    /// <summary>
    /// Implement on any GameObject/MonoBehaviour that can be interacted with by the player.
    /// Works with single-player PlayerInteractor.
    /// </summary>
    public interface IInteractable
    {
        /// <summary>Display name shown near the crosshair. e.g. "Sorting Basket", "Cardboard Box", "Upgrade Kiosk"</summary>
        string InteractableName { get; }

        /// <summary>
        /// Input prompts shown in the prompt stack while this is the current interactable.
        /// Return one entry per available action. e.g. { KeyE, "Deposit" }, { KeyF, "Sell" }
        /// </summary>
        List<InputPrompt> GetPrompts { get; }

        /// <summary>Seconds the player must hold. Return 0 for instant interactions.</summary>
        float HoldDuration { get; }

        void OnInteractStart(PlayerInteractor interactor);
        void OnInteractCanceled(PlayerInteractor interactor);
        void OnInteractComplete(PlayerInteractor interactor);
    }
}
