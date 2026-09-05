using System;

namespace Pinata.Tools
{
    public interface ITool
    {
        string ToolName { get; }
        int SlotIndex { get; }
        bool IsBusy { get; }

        void OnEquip();
        void OnUnequip();
    }
}
