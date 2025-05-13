using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows;

namespace gamevault.Helper
{
    public static class ToggleButtonGroupBehavior
    {
        public static readonly DependencyProperty GroupNameProperty =
            DependencyProperty.RegisterAttached(
                "GroupName",
                typeof(string),
                typeof(ToggleButtonGroupBehavior),
                new PropertyMetadata(null, OnGroupNameChanged));

        public static string GetGroupName(DependencyObject obj)
        {
            return (string)obj.GetValue(GroupNameProperty);
        }

        public static void SetGroupName(DependencyObject obj, string value)
        {
            obj.SetValue(GroupNameProperty, value);
        }

        public static ToggleButton GetCheckedToggleButton(string groupName, DependencyObject searchRoot)
        {
            if (searchRoot == null || string.IsNullOrEmpty(groupName))
                return null;

            foreach (var toggleButton in FindVisualChildren<ToggleButton>(searchRoot))
            {
                if (GetGroupName(toggleButton) == groupName && toggleButton.IsChecked == true)
                {
                    return toggleButton;
                }
            }

            return null;
        }

        public static void CheckToggleButtonByIndex(string groupName, DependencyObject searchRoot, int index)
        {
            if (searchRoot == null || string.IsNullOrEmpty(groupName) || index < 0)
                return;

            var toggleButtons = new List<ToggleButton>();

            // Find all toggle buttons in the specified group
            foreach (var toggleButton in FindVisualChildren<ToggleButton>(searchRoot))
            {
                if (GetGroupName(toggleButton) == groupName)
                {
                    toggleButtons.Add(toggleButton);
                }
            }

            // Check if index is valid
            if (index < toggleButtons.Count)
            {
                // Set the toggle button at the specified index to checked
                toggleButtons[index].IsChecked = true;

                // This will trigger the Checked event, which will uncheck other buttons in the group
                // due to the existing ToggleButton_Checked handler
            }
        }

        private static void OnGroupNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ToggleButton toggleButton)
            {
                if (e.NewValue != null)
                {
                    toggleButton.Checked += ToggleButton_Checked;
                    toggleButton.Unchecked += ToggleButton_Unchecked;
                }
                else
                {
                    toggleButton.Checked -= ToggleButton_Checked;
                    toggleButton.Unchecked -= ToggleButton_Unchecked;
                }
            }
        }

        private static void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton checkedButton)
            {
                string groupName = GetGroupName(checkedButton);
                if (groupName == null)
                    return;

                var parent = VisualTreeHelper.GetParent(checkedButton);
                while (parent != null && !(parent is Window))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent != null)
                {
                    var toggleButtons = FindVisualChildren<ToggleButton>(parent);
                    foreach (var button in toggleButtons)
                    {
                        if (button == checkedButton)
                            continue;

                        if (GetGroupName(button) == groupName)
                        {
                            button.IsChecked = false;
                        }
                    }
                }
            }
        }

        private static void ToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton uncheckedButton)
            {
                string groupName = GetGroupName(uncheckedButton);
                if (groupName == null)
                    return;

                var parent = VisualTreeHelper.GetParent(uncheckedButton);
                while (parent != null && !(parent is Window))
                {
                    parent = VisualTreeHelper.GetParent(parent);
                }

                if (parent != null)
                {
                    // Check if any other button in the group is checked
                    bool anyOtherChecked = false;
                    var toggleButtons = FindVisualChildren<ToggleButton>(parent);

                    foreach (var button in toggleButtons)
                    {
                        if (button != uncheckedButton && GetGroupName(button) == groupName && button.IsChecked == true)
                        {
                            anyOtherChecked = true;
                            break;
                        }
                    }

                    // If no other button is checked, prevent unchecking by setting IsChecked back to true
                    if (!anyOtherChecked)
                    {
                        // Use dispatcher to avoid potential event handling issues
                        uncheckedButton.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // Temporarily remove the event handler to avoid infinite loop
                            uncheckedButton.Unchecked -= ToggleButton_Unchecked;
                            uncheckedButton.IsChecked = true;
                            uncheckedButton.Unchecked += ToggleButton_Unchecked;
                        }));
                    }
                }
            }
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
                yield break;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                if (child is T t)
                    yield return t;

                foreach (var childOfChild in FindVisualChildren<T>(child))
                    yield return childOfChild;
            }
        }
    }
}
