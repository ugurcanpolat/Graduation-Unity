using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanelButtonHandler : MonoBehaviour
{
    public GameObject Panel;
    public GameObject Button;

    Animator panelAnimator;
    Animator buttonAnimator;

    void Start()
    {
        panelAnimator = Panel.GetComponent<Animator>();
        buttonAnimator = Button.GetComponent<Animator>();
    }

    public void TriggerHideOrShowAnimation()
    {
        if (panelAnimator != null && buttonAnimator != null)
        {
            bool isPanelHiding = panelAnimator.GetBool("hiding");
            bool isButtonHiding = buttonAnimator.GetBool("hiding");

            panelAnimator.SetBool("hiding", !isPanelHiding);
            buttonAnimator.SetBool("hiding", !isButtonHiding);
        }
    }

    private void HideOrShowPanel()
    {
        Panel.SetActive(!Panel.activeSelf);
    }
}
