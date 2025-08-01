using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace gamevault.Helper
{
    public static class PasswordBoxAttachedProperties
    {
        // Property to enable/disable password mode
        public static readonly DependencyProperty IsPasswordProperty =
            DependencyProperty.RegisterAttached("IsPassword", typeof(bool), typeof(PasswordBoxAttachedProperties),
                new PropertyMetadata(false, OnIsPasswordChanged));

        // Property to store the actual password (bindable)
        public static readonly DependencyProperty ActualPasswordProperty =
            DependencyProperty.RegisterAttached("ActualPassword", typeof(string), typeof(PasswordBoxAttachedProperties),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnActualPasswordChanged));

        // Property to store the password character (default is ●)
        public static readonly DependencyProperty PasswordCharProperty =
            DependencyProperty.RegisterAttached("PasswordChar", typeof(char), typeof(PasswordBoxAttachedProperties),
                new PropertyMetadata('●'));

        public static bool GetIsPassword(DependencyObject obj) => (bool)obj.GetValue(IsPasswordProperty);
        public static void SetIsPassword(DependencyObject obj, bool value) => obj.SetValue(IsPasswordProperty, value);

        public static string GetActualPassword(DependencyObject obj) => (string)obj.GetValue(ActualPasswordProperty);
        public static void SetActualPassword(DependencyObject obj, string value) => obj.SetValue(ActualPasswordProperty, value);

        public static char GetPasswordChar(DependencyObject obj) => (char)obj.GetValue(PasswordCharProperty);
        public static void SetPasswordChar(DependencyObject obj, char value) => obj.SetValue(PasswordCharProperty, value);

        private static void OnIsPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox) return;

            if ((bool)e.NewValue)
            {
                textBox.TextChanged += MaskPassword;
                textBox.PreviewTextInput += OnPreviewTextInput;
                textBox.PreviewKeyDown += OnPreviewKeyDown;
                DataObject.AddPastingHandler(textBox, OnPaste);

                if (textBox.IsLoaded)
                    UpdateText(textBox);
                else
                    textBox.Loaded += TextBox_Loaded;
            }
            else
            {
                textBox.TextChanged -= MaskPassword;
                textBox.PreviewTextInput -= OnPreviewTextInput;
                textBox.PreviewKeyDown -= OnPreviewKeyDown;
                DataObject.RemovePastingHandler(textBox, OnPaste);
                textBox.Loaded -= TextBox_Loaded;
            }
        }

        private static void OnActualPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBox textBox && GetIsPassword(textBox))
            {
                UpdateText(textBox);
            }
        }

        private static void TextBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.Loaded -= TextBox_Loaded;
                UpdateText(textBox);
            }
        }

        private static void UpdateText(TextBox textBox)
        {
            var actualText = GetActualPassword(textBox);
            var passwordChar = GetPasswordChar(textBox);

            textBox.TextChanged -= MaskPassword;
            textBox.Text = new string(passwordChar, actualText?.Length ?? 0);
            textBox.TextChanged += MaskPassword;
        }

        private static void MaskPassword(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox || !GetIsPassword(textBox))
                return;

            var passwordChar = GetPasswordChar(textBox);
            var currentText = textBox.Text;
            var actualPassword = GetActualPassword(textBox) ?? string.Empty;

            // If the text is being set to masked characters, ignore
            if (currentText == new string(passwordChar, actualPassword.Length))
                return;

            // Handle text changes
            textBox.TextChanged -= MaskPassword;

            int caretPos = textBox.CaretIndex;

            // Handle complete deletion (Ctrl+A then Delete or Backspace)
            if (string.IsNullOrEmpty(currentText))
            {
                SetActualPassword(textBox, string.Empty);
            }
            else if (currentText.Length > actualPassword.Length)
            {
                // Characters added (typing or pasting)
                int addedChars = currentText.Length - actualPassword.Length;
                int insertPosition = caretPos - addedChars;

                // Ensure we don't try to insert at negative position
                insertPosition = Math.Max(0, insertPosition);

                // Get the actual characters that were added (not masked chars)
                string newChars = currentText.Substring(insertPosition, addedChars)
                    .Replace(passwordChar.ToString(), ""); // Filter out any mask chars

                if (!string.IsNullOrEmpty(newChars))
                {
                    var newPassword = actualPassword.Insert(insertPosition, newChars);
                    SetActualPassword(textBox, newPassword);
                }
            }
            else if (currentText.Length < actualPassword.Length)
            {
                // Characters deleted
                int deletedCount = actualPassword.Length - currentText.Length;

                // Handle backspace/delete at different positions
                if (caretPos < actualPassword.Length)
                {
                    var newPassword = actualPassword.Remove(caretPos, deletedCount);
                    SetActualPassword(textBox, newPassword);
                }
                else
                {
                    // Handle deletion at end
                    var newPassword = actualPassword.Substring(0, actualPassword.Length - deletedCount);
                    SetActualPassword(textBox, newPassword);
                }
            }

            // Update the display
            textBox.Text = new string(passwordChar, GetActualPassword(textBox).Length);
            textBox.CaretIndex = caretPos;

            textBox.TextChanged += MaskPassword;
        }

        private static void OnPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox textBox || !GetIsPassword(textBox))
                return;

            // Cancel the standard paste operation
            e.CancelCommand();

            // Get the paste text and sanitize it
            string pasteText = (string)e.DataObject.GetData(typeof(string));
            if (string.IsNullOrEmpty(pasteText))
                return;

            // Remove any invalid characters (like the password mask character)
            var passwordChar = GetPasswordChar(textBox);
            pasteText = pasteText.Replace(passwordChar.ToString(), "");

            if (string.IsNullOrEmpty(pasteText))
                return;

            // Get current selection info
            int selectionStart = textBox.SelectionStart;
            int selectionLength = textBox.SelectionLength;
            string currentPassword = GetActualPassword(textBox) ?? string.Empty;

            // Handle the paste operation
            string newPassword;
            if (selectionLength > 0)
            {
                // Replace selected text
                newPassword = currentPassword.Remove(selectionStart, selectionLength)
                    .Insert(selectionStart, pasteText);
            }
            else
            {
                // Insert at cursor position
                newPassword = currentPassword.Insert(selectionStart, pasteText);
            }

            // Update the actual password
            SetActualPassword(textBox, newPassword);

            // Update the display
            textBox.Text = new string(passwordChar, newPassword.Length);
            textBox.CaretIndex = selectionStart + pasteText.Length;
        }

        private static void OnPreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Allow text input (handled in TextChanged)
            e.Handled = false;
        }

        private static void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not TextBox textBox || !GetIsPassword(textBox))
                return;

            // Prevent spaces in password
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
            // Handle Ctrl+A select all
            else if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                textBox.SelectAll();
                e.Handled = true;
            }
            // Handle Ctrl+V paste (we handle it ourselves in OnPaste)
            else if (e.Key == Key.V && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                // Let the paste handler deal with it
                e.Handled = false;
            }
        }
    }
}