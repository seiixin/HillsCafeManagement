using HillsCafeManagement.Helpers;
using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace HillsCafeManagement.ViewModels
{
    public class ReceiptsViewModel : INotifyPropertyChanged
    {
        // ----------- Bindable state -----------
        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                {
                    // live search; remove if you want explicit Search button only
                    LoadReceipts(_searchText);
                }
            }
        }

        private ReceiptsModel? _selected;
        public ReceiptsModel? Selected
        {
            get => _selected;
            set => Set(ref _selected, value);
        }

        public ObservableCollection<ReceiptsModel> Receipts { get; } = new();

        // ----------- Commands -----------
        public ICommand RefreshCommand { get; }
        public ICommand SearchCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand PrintCommand { get; }

        // ----------- Ctor -----------
        public ReceiptsViewModel()
        {
            RefreshCommand = new RelayCommand(_ => LoadReceipts());
            SearchCommand = new RelayCommand(_ => LoadReceipts(SearchText));
            ClearSearchCommand = new RelayCommand(_ => { SearchText = string.Empty; LoadReceipts(); });

            AddCommand = new RelayCommand(_ => AddReceipt(), _ => true);
            EditCommand = new RelayCommand(_ => EditReceipt(), _ => Selected != null);
            DeleteCommand = new RelayCommand(_ => DeleteReceipt(), _ => Selected != null);
            PrintCommand = new RelayCommand(_ => PrintReceipt(), _ => Selected != null);

            LoadReceipts();
        }

        // ----------- Data ops -----------
        public void LoadReceipts(string? term = null)
        {
            try
            {
                var data = ReceiptsServices.GetAllReceipts(term);
                Receipts.Clear();
                foreach (var r in data) Receipts.Add(r);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load receipts.\n{ex.Message}", "Receipts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddReceipt()
        {
            try
            {
                // Stub: replace with your editor dialog
                var newModel = new ReceiptsModel
                {
                    OrderId = 0,   // TODO from UI
                    Amount = 0m   // TODO from UI
                };

                var newId = ReceiptsServices.Create(newModel);
                var created = ReceiptsServices.GetById(newId);
                if (created != null) Receipts.Insert(0, created);
                Selected = created;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to create receipt.\n{ex.Message}", "Receipts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditReceipt()
        {
            if (Selected == null) return;

            try
            {
                ReceiptsServices.Update(Selected);

                var updated = ReceiptsServices.GetById(Selected.ReceiptId);
                if (updated != null)
                {
                    var idx = Receipts.ToList().FindIndex(r => r.ReceiptId == updated.ReceiptId);
                    if (idx >= 0) Receipts[idx] = updated;
                    Selected = updated;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update receipt.\n{ex.Message}", "Receipts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteReceipt()
        {
            if (Selected == null) return;

            var confirm = MessageBox.Show(
                $"Delete receipt #{Selected.ReceiptId}?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                ReceiptsServices.Delete(Selected.ReceiptId);
                var toRemove = Selected;
                Selected = null;
                var hit = Receipts.FirstOrDefault(r => r.ReceiptId == toRemove.ReceiptId);
                if (hit != null) Receipts.Remove(hit);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to delete receipt.\n{ex.Message}", "Receipts",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ----------- Print / Export flow -----------
        private void PrintReceipt()
        {
            if (Selected == null)
            {
                MessageBox.Show("Select a receipt first.", "Print",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var r = Selected;

            // 1) Info popup
            var info =
                $"Receipt ID : {r.ReceiptId}\n" +
                $"Order ID   : {r.OrderId}\n" +
                $"Table      : {r.TableNumber}\n" +
                $"Date       : {r.Date}\n" +
                $"Amount     : {r.Amount:0.##}\n\n" +
                "Do you want to export this receipt?";
            MessageBox.Show(info, "Receipt Info", MessageBoxButton.OK, MessageBoxImage.Information);

            // 2) Confirmation & choice (Yes=PDF, No=JPG)
            var choice = MessageBox.Show(
                "Choose export format:\n\nYes = PDF (via Print dialog)\nNo = JPG (save image)\nCancel = Do nothing",
                "Export Receipt",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (choice == MessageBoxResult.Cancel) return;

            try
            {
                if (choice == MessageBoxResult.Yes)
                    ExportReceiptAsPdf(r);
                else if (choice == MessageBoxResult.No)
                    ExportReceiptAsJpg(r);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed.\n{ex.Message}", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportReceiptAsPdf(ReceiptsModel r)
        {
            // Build simple printable visual
            var visual = BuildReceiptVisual(r);

            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                // Size to printable area
                var size = new Size(dlg.PrintableAreaWidth, dlg.PrintableAreaHeight);
                visual.Measure(size);
                visual.Arrange(new Rect(new Point(0, 0), size));
                visual.UpdateLayout();

                // User can pick "Microsoft Print to PDF" to generate a PDF
                dlg.PrintVisual(visual, $"Receipt #{r.ReceiptId}");
            }
        }

        private void ExportReceiptAsJpg(ReceiptsModel r)
        {
            var visual = BuildReceiptVisual(r);

            // Render at friendly size
            double width = 800, height = 600;
            visual.Measure(new Size(width, height));
            visual.Arrange(new Rect(0, 0, width, height));
            visual.UpdateLayout();

            var rtb = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
            rtb.Render(visual);

            var encoder = new JpegBitmapEncoder { QualityLevel = 92 };
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            var sfd = new SaveFileDialog
            {
                Title = "Save Receipt as JPG",
                Filter = "JPEG Image (*.jpg)|*.jpg",
                FileName = $"receipt_{r.ReceiptId}.jpg"
            };

            if (sfd.ShowDialog() == true)
            {
                using var fs = new FileStream(sfd.FileName, FileMode.Create, FileAccess.Write);
                encoder.Save(fs);
                MessageBox.Show("JPG saved successfully.", "Export",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private FrameworkElement BuildReceiptVisual(ReceiptsModel r)
        {
            var root = new Grid
            {
                Background = Brushes.White,
                Margin = new Thickness(24)
            };

            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (int i = 0; i < 6; i++)
                root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = "Hills Café — Receipt",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 16)
            };

            var line1 = new TextBlock { Text = $"Receipt ID : {r.ReceiptId}", FontSize = 16, Margin = new Thickness(0, 4, 0, 4) };
            var line2 = new TextBlock { Text = $"Order ID   : {r.OrderId}", FontSize = 16, Margin = new Thickness(0, 4, 0, 4) };
            var line3 = new TextBlock { Text = $"Table      : {r.TableNumber}", FontSize = 16, Margin = new Thickness(0, 4, 0, 4) };
            var line4 = new TextBlock { Text = $"Date       : {r.Date}", FontSize = 16, Margin = new Thickness(0, 4, 0, 4) };
            var line5 = new TextBlock { Text = $"Amount     : {r.Amount:0.##}", FontSize = 16, Margin = new Thickness(0, 4, 0, 4) };
            var footer = new TextBlock
            {
                Text = "Thank you for dining with us!",
                FontSize = 14,
                Margin = new Thickness(0, 18, 0, 0),
                Opacity = 0.8
            };

            Grid.SetRow(title, 0);
            Grid.SetRow(line1, 1);
            Grid.SetRow(line2, 2);
            Grid.SetRow(line3, 3);
            Grid.SetRow(line4, 4);
            Grid.SetRow(line5, 5);
            Grid.SetRow(footer, 6);

            root.Children.Add(title);
            root.Children.Add(line1);
            root.Children.Add(line2);
            root.Children.Add(line3);
            root.Children.Add(line4);
            root.Children.Add(line5);
            root.Children.Add(footer);

            return root;
        }

        // ----------- INotifyPropertyChanged -----------
        public event PropertyChangedEventHandler? PropertyChanged;
        protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            return true;
        }
    }
}
