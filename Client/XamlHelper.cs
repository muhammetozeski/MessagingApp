using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Xml;
using System.Windows.Markup;
using System.Windows.Media;

namespace ClientSpace
{
    internal static class XamlHelper
    {
        public static void CopyGridContent(Grid sourceGrid, ListBox parent)
        {
            List<UIElement> clonedElements = new List<UIElement>();

            foreach (UIElement element in sourceGrid.Children)
            {
                if (element is FrameworkElement frameworkElement)
                {
                    // FrameworkElement türündeki öğeleri kopyala
                    FrameworkElement clonedElement = CloneFrameworkElement(frameworkElement);
                    clonedElements.Add(clonedElement);
                }
            }

            // Kopyalanan öğeleri hedef ListBox'a ekle
            foreach (UIElement clonedElement in clonedElements)
            {
                parent.Items.Add(clonedElement);
            }
        }

        public static FrameworkElement CopyUIElement(FrameworkElement sourceElement, ItemsControl parent)
        {
            // Kopyalanacak öğe türünü belirleyin
            Type elementType = sourceElement.GetType();

            // Kopyalanan öğeyi oluşturun
            FrameworkElement clonedElement = CloneFrameworkElement(sourceElement);
            parent.Items.Add(clonedElement);
            return clonedElement;
        }

        static private FrameworkElement CloneFrameworkElement(FrameworkElement source)
        {
            // XAML üzerinden kopyalama işlemi
            string xamlString = XamlWriter.Save(source);
            StringReader stringReader = new StringReader(xamlString);
            XmlReader xmlReader = XmlReader.Create(stringReader);
            FrameworkElement clonedElement = XamlReader.Load(xmlReader) as FrameworkElement;

            return clonedElement;
        }
        public static T FindChildByTag<T>(this DependencyObject parent, int tagValue) where T : FrameworkElement
        {
            if (parent == null)
                return null;

            var count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
                if (child != null)
                {
                    if (child.Tag is int && (int)child.Tag == tagValue && child is T)
                    {
                        return (T)child;
                    }

                    var result = FindChildByTag<T>(child, tagValue);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }
    }
}
