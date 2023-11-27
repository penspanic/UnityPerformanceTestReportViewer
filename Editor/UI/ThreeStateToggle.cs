using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.UI
{
    public class ThreeStateToggle : VisualElement
    {
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
                    checkMarkImage.image = null;
                    break;
                case StateType.MiddleState:
                    checkMarkImage.image = AssetDatabase.LoadAssetAtPath<Texture>($"{Constants.SpritesPath}/middleState.png");
                    break;
                case StateType.Checked:
                    checkMarkImage.image = AssetDatabase.LoadAssetAtPath<Texture>($"{Constants.SpritesPath}/checked.png");
                    break;
            }
        }
    }
}