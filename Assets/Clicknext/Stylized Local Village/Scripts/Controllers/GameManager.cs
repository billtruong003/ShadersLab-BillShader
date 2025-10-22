using Clicknext.StylizedLocalVillage.Characters;
using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI;
using Clicknext.StylizedLocalVillage.UI.Dialogs;
using Clicknext.StylizedLocalVillage.Units;
using UnityEngine;

namespace Clicknext.StylizedLocalVillage.Controllers
{
    public class GameManager : MonoBehaviour
    {
        private Farmer _player;
        private Customer[] _customers;
        private Area[] _allArea;
        private Shelf[] _allShelves;

        private Area _currentArea;
        private OrderPopup _OrderDialog;
        private ShelfLabels _shelfLabels;
        private FloatingAmounts _floatingAmounts;
        private ThinkingBalloons _thinkingBalloons;
        private TransactionDialog _transactionDialog;
        private LevelDialog _levelDialog;

        private void Awake()
        {
            _player = FindAnyObjectByType<Farmer>();
            _OrderDialog = FindAnyObjectByType<OrderPopup>();
            _shelfLabels = FindAnyObjectByType<ShelfLabels>();
            _floatingAmounts = FindAnyObjectByType<FloatingAmounts>();
            _thinkingBalloons = FindAnyObjectByType<ThinkingBalloons>();
            _transactionDialog = FindAnyObjectByType<TransactionDialog>();
            _levelDialog = FindAnyObjectByType<LevelDialog>();

            _customers = FindObjectsByType<Customer>(FindObjectsSortMode.None);
            _allArea = FindObjectsByType<Area>(FindObjectsSortMode.None);
            _allShelves = FindObjectsByType<Shelf>(FindObjectsSortMode.None);

            _OrderDialog.OnGatheredProduct += OnGatherProduct;

            foreach (var buyer in _customers)
            {
                buyer.OnBought += OnCustomerBought;
            }

            foreach (var area in _allArea)
            {
                area.OnEnter += (collider, products, product) => OnEnterArea(collider, area, products, product);
                area.OnExit += OnExistArea;
            }

            foreach (var shelf in _allShelves)
            {
                shelf.OnEnter += (collider, product) => OnEnterShelf(collider, shelf, product.Type);
            }
        }

        private void OnCustomerBought(Customer customer, Product product, int amount)
        {
            if (amount == 0) {
                _thinkingBalloons.Create(product.Type, customer.AttachedPoint);
                _transactionDialog.LogOutofStock(product.Type.ToString());
            }
            else
            {
                var price = Market.GetPrice(product.Type, product.IngredientCount);
                var total = amount * price;
                    Vault.Coin += total;
                var exp = _levelDialog.GenerateExp();

                _floatingAmounts.Create(Currency.Coin, total, customer.AttachedPoint.position);
                _transactionDialog.LogSold(product.Type.ToString(), amount, total, exp);
            }
        }

        private void OnEnterShelf(Collider collider, Shelf shelf, Item targetType)
        {
            if (collider.gameObject != _player.gameObject)
                return;

            if (targetType == shelf.Product.Type)
            {
                var amount = _player.GetItemByType(targetType);
                shelf.Add(amount);

                if (amount != 0)
                {
                    var exp = _levelDialog.GenerateExp();
                    _transactionDialog.LogGather(targetType.ToString(), amount, exp);

                    _floatingAmounts.Create(targetType, amount, _player.AttachedPoint.position);
                    _shelfLabels.ShowLabels(targetType, isVisible: false);
                }
            }
        }

        private void OnEnterArea(Collider collider, Area area, Product[] products, Product product)
        {
            if (collider.gameObject != _player.gameObject)
                return;

            _currentArea = area;
            _OrderDialog.Visible(area.AttachedPoint.position, product, products, OnChangedProduct);
        }

        private void OnExistArea(Collider collider)
        {
            if (collider.gameObject != _player.gameObject)
                return;

            _OrderDialog.Hide();
        }

        private void OnGatherProduct()
        {
            var amount = _currentArea.GatherProduct();
            if (amount != 0)
                OnFarmerPickItem(_currentArea.ProductType, amount);
        }

        private void OnChangedProduct(Product product)
        {
            var previousProduct = _currentArea.ProductType;
            var amount = _currentArea.ChangeAndGatherProduct(product);
            if (amount != 0)
                OnFarmerPickItem(previousProduct, amount);
        }

        private void OnFarmerPickItem(Item item, int amount)
        {
            _player.Pick(_currentArea.AttachedPoint, item, amount);
            _floatingAmounts.Create(Currency.Bag, amount, _player.AttachedPoint.position);
            _shelfLabels.ShowLabels(item, isVisible: true);
        }

        private void FixedUpdate()
        {
            if (_OrderDialog && _currentArea)
                _OrderDialog.UpdateInformation(_currentArea.Amount, _currentArea.ProducTime);
        }
    }
}
