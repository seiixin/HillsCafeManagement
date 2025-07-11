using HillsCafeManagement.Models;
using HillsCafeManagement.Services;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace HillsCafeManagement.ViewModels
{
    public class ReceiptsViewModel
    {
        public ObservableCollection<ReceiptsModel> Receipts { get; set; }

        public ICommand PrintCommand { get; }

        public ReceiptsViewModel()
        {
            var data = ReceiptsServices.GetAllReceipts();
            Receipts = new ObservableCollection<ReceiptsModel>(data);
            PrintCommand = new RelayCommand<ReceiptsModel>(PrintReceipt);
        }

        private void PrintReceipt(ReceiptsModel receipt)
        {
            // Implement print logic, for now, show a message
            System.Windows.MessageBox.Show($"Printing Receipt ID: {receipt.ReceiptId}");
        }
    }
}
