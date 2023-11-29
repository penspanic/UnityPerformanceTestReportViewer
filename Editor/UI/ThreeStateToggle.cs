using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.Editor.UI
{
    public class ThreeStateToggle : VisualElement
    {
        public class Classes
        {
            public const string checkmark_unchecked = "threestate-toggle-checkmark-unchecked";
            public const string checkmark_checked = "threestate-toggle-checkmark-checked";
            public const string checkmark_middlestate = "threestate-toggle-checkmark-middlestate";
        }

        private new class UxmlTraits : VisualElement.UxmlTraits
        {
            UxmlEnumAttributeDescription<StateType> m_StateAttribute = new(){ name = "state" };
            UxmlStringAttributeDescription m_TextAttribute = new(){ name = "text" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);

                (ve as ThreeStateToggle).State = m_StateAttribute.GetValueFromBag(bag, cc);
                (ve as ThreeStateToggle).Text = m_TextAttribute.GetValueFromBag(bag, cc);
            }
        }

        private new class UxmlFactory : UxmlFactory<ThreeStateToggle, UxmlTraits>
        {
        }

        public enum StateType
        {
            Unchecked,
            MiddleState,
            Checked,
        }

        private static readonly string layoutPath = $"{Constants.LayoutPath}/{nameof(ThreeStateToggle)}.uxml";

        public event Action<StateType> OnStateChanged;
        public StateType State
        {
            get => _state;
            set
            {
                _state = value;
                RefreshImage();
                MarkDirtyRepaint();
                OnStateChanged?.Invoke(_state);
            }
        }
        private StateType _state;

        public string Text
        {
            get => label.text;
            set
            {
                label.text = value;
            }
        }

        public Button SettingButton => settingButton;

        private string _text;


        private Label label;
        private Image checkMarkImage;
        private Button settingButton;

        public ThreeStateToggle()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);

            label = this.Q<Label>(nameof(label));
            checkMarkImage = this.Q<Image>(nameof(checkMarkImage));
            settingButton = this.Q<Button>(nameof(settingButton));

            checkMarkImage.RegisterCallback((ClickEvent clickEvent) =>
            {
                if (State == StateType.Checked)
                    State = StateType.Unchecked;
                else
                    State = StateType.Checked;
            });
        }

        private void RefreshImage()
        {
            switch (_state)
            {
                case StateType.Unchecked:
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_checked);
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_middlestate);

                    checkMarkImage.AddToClassList(Classes.checkmark_unchecked);
                    break;
                case StateType.MiddleState:
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_checked);
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_unchecked);

                    checkMarkImage.AddToClassList(Classes.checkmark_middlestate);
                    break;
                case StateType.Checked:
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_unchecked);
                    checkMarkImage.RemoveFromClassList(Classes.checkmark_middlestate);

                    checkMarkImage.AddToClassList(Classes.checkmark_checked);
                    break;
            }
        }
    }
}