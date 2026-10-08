using UnityEditor.IMGUI.Controls;

namespace TEngine.Editor
{
    internal sealed class AssetViewItem : TreeViewItem<int>
    {
        public ReferenceFinderData.AssetDescription data;
    }
}