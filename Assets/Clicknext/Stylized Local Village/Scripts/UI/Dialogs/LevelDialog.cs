using Clicknext.StylizedLocalVillage.Entities;
using Clicknext.StylizedLocalVillage.UI.Templates;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Clicknext.StylizedLocalVillage.UI.Dialogs
{
    public class LevelDialog : MonoBehaviour
    {
        [Serializable]
        private class LevelData
        {
            public int expEach;
            public ProductReward rewardProduct;
            public int rewardCoin;
            public int rewardExp;
            public bool received;
        }

        [Serializable]
        private class ProductReward
        {
            public Product product;
            public int amount;
        }

        [SerializeField] int minExp;
        [SerializeField] int maxExp;
        [SerializeField] GameObject dialog;
        [SerializeField] TMP_Text levelText;
        [SerializeField] TMP_Text levelInfoText;
        [SerializeField] TMP_Text expText;
        [SerializeField] Button openButton;
        [SerializeField] Button closeButton;
        [SerializeField] Slider expSlider;
        [SerializeField] GameObject notification;
        [SerializeField] RewardTemplate rewardTemplate;
        [SerializeField] LevelData[] levelData;

        private readonly Dictionary<int, RewardTemplate> _rewards =  new();

        void Awake()
        {
            openButton.onClick.AddListener(() => dialog.SetActive(true));
            closeButton.onClick.AddListener(() =>
            {
                dialog.SetActive(false);
                notification.SetActive(false);
            });
            dialog.SetActive(true);

            Vault.OnExpChanged += SetLevel;

            SetLevel();
            SetRewards();
        }

        void SetLevel()
        {
            int sumExp = 0;
            var currentLevel = levelData[Vault.Level - 1];

            sumExp += currentLevel.expEach;
            var exp = Vault.EXP - currentLevel.expEach;
            if (exp >= 0)
            {
                if (Vault.Level < levelData.Length)
                    Vault.Level++;
                Vault.EXP = exp;
            }

            if (!currentLevel.received)
            {
                notification.SetActive(true);
                currentLevel.received = true;

                if(_rewards.ContainsKey(Vault.Level))
                    _rewards[Vault.Level].SetReceived(true);

                Vault.EXP += currentLevel.rewardExp;
                Vault.Coin += currentLevel.rewardCoin;

                if (currentLevel.rewardProduct != null && currentLevel.rewardProduct.product != null)
                {
                    var type = currentLevel.rewardProduct.product.Type;
                    var amount = currentLevel.rewardProduct.amount;
                    Storage.Add(type, amount);
                }
            }

            levelText.text = Vault.Level.ToString();
            levelInfoText.text = levelText.text;

            expText.text = $"{Vault.EXP}/{sumExp}";
            expSlider.value = sumExp > 0? Vault.EXP / (float)sumExp : 0f;
        }

        void SetRewards() 
        { 
            for(int i = 0; i < levelData.Length; ++i)
            {
                var data = levelData[i];

                if (data.rewardExp == 0 && 
                    data.rewardCoin == 0 && 
                    data.rewardProduct.product == null)
                    continue;

                var levelstr = $"Lv.{i + 1}";

                var amount = data.rewardCoin > 0 ? data.rewardCoin :
                        data.rewardExp > 0 ? data.rewardExp :
                        data.rewardProduct.amount;

                var path = data.rewardCoin > 0 ? Currency.Coin.ToString() :
                        data.rewardExp > 0 ? Currency.EXP.ToString() :
                        data.rewardProduct.product.Type.ToString();

                var scale = data.rewardProduct.product != null? 1.2f: 1f;

                var reward = Instantiate(rewardTemplate, rewardTemplate.transform.parent);
                    reward.Set(levelstr, amount.ToString(), path, scale);
                _rewards.Add(i+1, reward);
            }

            rewardTemplate.transform.SetAsLastSibling();
            _rewards.Add(levelData.Length, rewardTemplate);
        }

        public int GenerateExp()
        {
            var exp = UnityEngine.Random.Range(minExp, maxExp);
            Vault.EXP += exp;
            return exp;
        }
    }
}
