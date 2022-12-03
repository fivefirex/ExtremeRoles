﻿using System;

using TMPro;
using UnityEngine;

using UnhollowerBaseLib.Attributes;

using ExtremeRoles.Module;

namespace ExtremeSkins.Module
{
    [Il2CppRegister]
    public sealed class CreatorButton : MonoBehaviour
    {
        private Scroller tabScroller;
        private TMP_Text creatorText;

        [HideFromIl2Cpp]
        public void Initialize(Scroller scroller, TMP_Text text)
        {
            this.tabScroller = scroller;
            this.creatorText = text;
        }

        [HideFromIl2Cpp]
        public Action GetClickAction()
        {
            return (() =>
            {
                Vector3 curScrollPos = this.tabScroller.Inner.transform.localPosition;
                Vector3 textPos = this.creatorText.transform.position;
                ExtremeSkinsPlugin.Logger.LogInfo($"Scroll from:{curScrollPos} to:{textPos}");
                this.tabScroller.Inner.transform.localPosition = new Vector3(
                    curScrollPos.x,
                    curScrollPos.y - textPos.y + 1.0f, // オフセット値
                    curScrollPos.z);
                this.tabScroller.UpdateScrollBars();
            });
        }
    }
}