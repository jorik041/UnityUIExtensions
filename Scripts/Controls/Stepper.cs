/// Credit David Gileadi
/// Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/pull-requests/11

using System;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UnityEngine.UI.Extensions
{
    // Stepper control
    [AddComponentMenu("UI/Extensions/Stepper")]
    [RequireComponent(typeof(RectTransform))]
    public class Stepper : UIBehaviour
    {
        public Button[] sides
        {
            get
            {
                if (_sides == null || _sides.Length == 0)
                {
                    _sides = GetSides();
                }
                return _sides;
            }
        }
        private Button[] _sides;

        [SerializeField]
        private int _value = 0;
        public int value { get { return _value; } set { _value = value; } }

        [SerializeField]
        private int _minimum = 0;
        public int minimum { get { return _minimum; } set { _minimum = value; } }

        [SerializeField]
        private int _maximum = 100;
        public int maximum { get { return _maximum; } set { _maximum = value; } }

        [SerializeField]
        private int _step = 1;
        public int step { get { return _step; } set { _step = value; } }

        [SerializeField]
        private bool _wrap = false;
        public bool wrap { get { return _wrap; } set { _wrap = value; } }

        [SerializeField]
        private Graphic _separator;
        public Graphic separator { get { return _separator; } set { _separator = value; _separatorWidth = 0; LayoutSides(sides); } }

        private float _separatorWidth = 0;
        private float separatorWidth
        {
            get
            {
                if (_separatorWidth == 0 && separator)
                {
                    _separatorWidth = separator.rectTransform.rect.width;
                    var image = separator.GetComponent<Image>();
                    if (image)
                        _separatorWidth /= image.pixelsPerUnit;
                }
                return _separatorWidth;
            }
        }

        [Serializable]
        public class StepperValueChangedEvent : UnityEvent<int> { }

        // Event delegates triggered on click.
        [SerializeField]
        private StepperValueChangedEvent _onValueChanged = new StepperValueChangedEvent();
        public StepperValueChangedEvent onValueChanged
        {
            get { return _onValueChanged; }
            set { _onValueChanged = value; }
        }

        protected Stepper()
        { }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (separator)
                LayoutSides();

            if (!wrap)
            {
                DisableAtExtremes(sides);
            }
        }
#endif

        private Button[] GetSides()
        {
            var buttons = GetComponentsInChildren<Button>();
            if (buttons.Length != 2)
            {
                throw new InvalidOperationException("A stepper must have two Button children");
            }

            for (int i = 0; i < 2; i++)
            {
                var side = buttons[i].GetComponent<StepperSide>();
                if (side == null)
                {
                    side = buttons[i].gameObject.AddComponent<StepperSide>();
                }
            }

            if (!wrap)
            {
                DisableAtExtremes(buttons);
            }
            LayoutSides(buttons);

            return buttons;
        }

        public void StepUp()
        {
            Step(step);
        }

        public void StepDown()
        {
            Step(-step);
        }

        private void Step(int amount)
        {
            value += amount;

            if (wrap)
            {
                if (value > maximum) value = minimum;
                if (value < minimum) value = maximum;
            }
            else
            {
                value = Math.Max(minimum, value);
                value = Math.Min(maximum, value);

                DisableAtExtremes(sides);
            }

            _onValueChanged.Invoke(value);
        }

        private void DisableAtExtremes(Button[] sides)
        {
            sides[0].interactable = wrap || value > minimum;
            sides[1].interactable = wrap || value < maximum;
        }

        private void RecreateSprites(Button[] sides)
        {
            for (int i = 0; i < 2; i++)
            {
                if (sides[i].image == null)
                    continue;

                var sprite = sides[i].image.sprite;
                if (sprite.border.x == 0 || sprite.border.z == 0)
                    continue;

                var rect = sprite.rect;
                var border = sprite.border;

                if (i == 0)
                {
                    rect.xMax = border.z;
                    border.z = 0;
                }
                else
                {
                    rect.xMin = border.x;
                    border.x = 0;
                }

                sides[i].image.sprite = Sprite.Create(sprite.texture, rect, sprite.pivot, sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, border);
            }
        }

        public void LayoutSides(Button[] sides = null)
        {
            sides = sides ?? this.sides;

            RecreateSprites(sides);

            RectTransform transform = this.transform as RectTransform;
            float width = (transform.rect.width / 2) - separatorWidth;

            for (int i = 0; i < 2; i++)
            {
                float insetX = i == 0 ? 0 : width + separatorWidth;

                var rectTransform = sides[i].GetComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.zero;
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, insetX, width);
                rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, transform.rect.height);

// TODO: maybe adjust text position
            }

            if (separator)
            {
                var sepTransform = gameObject.transform.Find("Separator");
                Graphic sep = (sepTransform != null) ? sepTransform.GetComponent<Graphic>() : (GameObject.Instantiate(separator.gameObject) as GameObject).GetComponent<Graphic>();
                sep.gameObject.name = "Separator";
                sep.gameObject.SetActive(true);
                sep.rectTransform.SetParent(this.transform, false);
                sep.rectTransform.anchorMin = Vector2.zero;
                sep.rectTransform.anchorMax = Vector2.zero;
                sep.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Left, width, separatorWidth);
                sep.rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0, transform.rect.height);
            }
        }
    }

    [RequireComponent(typeof(Button))]
    public class StepperSide : UIBehaviour, IPointerClickHandler, ISubmitHandler
    {
        Button button { get { return GetComponent<Button>(); } }

        Stepper stepper { get { return GetComponentInParent<Stepper>(); } }

        bool leftmost { get { return button == stepper.sides[0]; } }

        protected StepperSide()
        { }

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            Press();
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            Press();
        }

        private void Press()
        {
            if (!button.IsActive() || !button.IsInteractable())
                return;

            if (leftmost)
            {
                stepper.StepDown();
            }
            else
            {
                stepper.StepUp();
            }
        }
    }
}