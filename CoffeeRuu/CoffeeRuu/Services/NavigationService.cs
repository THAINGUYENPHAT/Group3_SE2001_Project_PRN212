
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace CoffeeRuu.Services
{
    public class NavigationService
    {
        private readonly Frame _frame;
        private readonly Dictionary<string, Func<object>> _views = new();

        public NavigationService(Frame frame)
        {
            _frame = frame;
        }

        public void Register(string name, Func<object> viewFactory)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException(
                    "Tên màn hình không được để trống.",
                    nameof(name));
            }

            _views[name] = viewFactory
                ?? throw new ArgumentNullException(nameof(viewFactory));
        }

        public void Navigate(string name)
        {
            if (!_views.TryGetValue(name, out var viewFactory))
            {
                throw new InvalidOperationException(
                    $"Chưa đăng ký màn hình: {name}");
            }

            object view = viewFactory();

            if (_frame.Content != null &&
                _frame.Content.GetType() == view.GetType())
            {
                return;
            }

            _frame.Navigate(view);
        }
    }
}
