using UnityEngine;
using System.Collections;

namespace UElixir.Sample.UI
{
    public abstract class UIElement : MonoBehaviour
    {
        public void SetVisibility(bool isVisible)
        {
            gameObject.SetActive(isVisible);
        }
    }
}
