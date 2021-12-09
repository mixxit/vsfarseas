using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace vsfarseas.src
{
    public class GuiRequisition : GuiDialog
    {
        public GuiRequisition(ICoreClientAPI capi, string requisitionBody) : base(capi)
        {
            ComposeDialog(requisitionBody);
        }

        public override string ToggleKeyCombinationCode => null;

        public override string DebugName => base.DebugName;

        public override float ZSize => base.ZSize;

        public override bool Focused => base.Focused;

        public override bool Focusable => base.Focusable;

        public override EnumDialogType DialogType => base.DialogType;

        public override double DrawOrder => base.DrawOrder;

        public override double InputOrder => base.InputOrder;

        public override bool UnregisterOnClose => base.UnregisterOnClose;

        public override bool PrefersUngrabbedMouse => base.PrefersUngrabbedMouse;

        public override bool DisableMouseGrab => base.DisableMouseGrab;

        public override bool CaptureAllInputs()
        {
            return base.CaptureAllInputs();
        }

        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override void Focus()
        {
            base.Focus();
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool IsOpened()
        {
            return base.IsOpened();
        }

        public override bool IsOpened(string dialogComposerName)
        {
            return base.IsOpened(dialogComposerName);
        }

        public override void OnBeforeRenderFrame3D(float deltaTime)
        {
            base.OnBeforeRenderFrame3D(deltaTime);
        }

        public override void OnBlockTexturesLoaded()
        {
            base.OnBlockTexturesLoaded();
        }

        public override bool OnEscapePressed()
        {
            return base.OnEscapePressed();
        }

        public override void OnFinalizeFrame(float dt)
        {
            base.OnFinalizeFrame(dt);
        }

        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
        }

        public override void OnGuiOpened()
        {

        }

        private void ComposeDialog(string requisitionBody)
        {
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle);

            // Just a simple 300x100 pixel box with 40 pixels top spacing for the title bar
            ElementBounds textBounds = ElementBounds.Fixed(0, 40, 300, 300);

            // Background boundaries. Again, just make it fit it's child elements, then add the text as a child element
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(textBounds);

            SingleComposer = capi.Gui.CreateCompo("myAwesomeDialog", dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar("Requisition", CloseIconPressed)
                .AddStaticText(requisitionBody, CairoFont.WhiteDetailText(), textBounds)
                .Compose()
            ;

        }
        private void CloseIconPressed()
        {
            TryClose();
        }


        public override void OnKeyDown(KeyEvent args)
        {
            base.OnKeyDown(args);
        }

        public override void OnKeyPress(KeyEvent args)
        {
            base.OnKeyPress(args);
        }

        public override void OnKeyUp(KeyEvent args)
        {
            base.OnKeyUp(args);
        }

        public override void OnLevelFinalize()
        {
            base.OnLevelFinalize();
        }

        public override bool OnMouseClickSlot(ItemSlot itemSlot)
        {
            return base.OnMouseClickSlot(itemSlot);
        }

        public override void OnMouseDown(MouseEvent args)
        {
            base.OnMouseDown(args);
        }

        public override bool OnMouseEnterSlot(ItemSlot slot)
        {
            return base.OnMouseEnterSlot(slot);
        }

        public override bool OnMouseLeaveSlot(ItemSlot itemSlot)
        {
            return base.OnMouseLeaveSlot(itemSlot);
        }

        public override void OnMouseMove(MouseEvent args)
        {
            base.OnMouseMove(args);
        }

        public override void OnMouseUp(MouseEvent args)
        {
            base.OnMouseUp(args);
        }

        public override void OnMouseWheel(MouseWheelEventArgs args)
        {
            base.OnMouseWheel(args);
        }

        public override void OnOwnPlayerDataReceived()
        {
            base.OnOwnPlayerDataReceived();
        }

        public override void OnRenderGUI(float deltaTime)
        {
            base.OnRenderGUI(deltaTime);
        }

        public override bool RequiresUngrabbedMouse()
        {
            return base.RequiresUngrabbedMouse();
        }

        public override bool ShouldReceiveKeyboardEvents()
        {
            return base.ShouldReceiveKeyboardEvents();
        }

        public override bool ShouldReceiveMouseEvents()
        {
            return base.ShouldReceiveMouseEvents();
        }

        public override bool ShouldReceiveRenderEvents()
        {
            return base.ShouldReceiveRenderEvents();
        }

        public override void Toggle()
        {
            base.Toggle();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public override bool TryClose()
        {
            return base.TryClose();
        }

        public override bool TryOpen()
        {
            return base.TryOpen();
        }

        public override void UnFocus()
        {
            base.UnFocus();
        }

        protected override void OnFocusChanged(bool on)
        {
            base.OnFocusChanged(on);
        }
    }
}
