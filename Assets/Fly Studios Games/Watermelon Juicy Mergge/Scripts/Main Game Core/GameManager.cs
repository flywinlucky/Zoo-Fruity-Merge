using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;

namespace WatermelonGameClone
{
    public class GameManager : MonoBehaviour
    {
        public event Action OnSphereDrop;

        public bool isActiveMinimOanePopUp;
        public WindowSkinUnlocked skinUnlocked;

        public GameObject unlockBigItemPopUpGameobject;
        public UnlockBigItemPopUp unlockBigItemPopUp;

        [ReadOnly]
        public Sphere unlockBigItemSphere;

        //[ReadOnly]
        public List<string> bigItemsUnlocked = new List<string>();
        [Space]

        public ComboSystem comboSystem;
        public SettingsManager settingsManager;

        public Animator popUpCurentItemBouncing;

        public MoveAxe moveAxe;
        public GameObject gameOverPanel;

        public SetSpriteUi[] setSpriteUi;

        public List<TwenScaling> twenScalingItem = new List<TwenScaling>();

        //public GameObject setSpritePrefab;
        //public Transform spawnPosition;

        public AudioSource drop_sfx, merge_sfx;

        [Header("Encouraging Text Settings")]
        [Space]
        public GameObject encouragingTextPrefab; // Prefab for the encouraging text
        public Transform encouragingTextInstantiatePosition; // Position to instantiate the encouraging text

        [Header("Score 3D Text Settings")]
        [Space]
        public GameObject score3DTextPrefab; // Prefab for the encouraging text
        public Transform score3DTextInstantiatePosition; // Position to instantiate the encouraging text

        [ReadOnly]
        public int hitsCount; // Number of hits from raycasting

        // Use a Dictionary for efficient item lookups by name
        [Header("Collected Items Data")]
        [Space]
        public Dictionary<string, SaveData> savedItemsData = new Dictionary<string, SaveData>();

        public Transform sphereSpawnPosition;

        [Header("Mission Requirements")]
        [Space]
        public List<SaveData> savedSpheresData = new List<SaveData>();
        [Header("Mission Requirements")]
        [Space]
        public List<SpheresCategory> spheresCategory = new List<SpheresCategory>();

        [Header("Objects")]
        //[SerializeField] private Sphere[] _spherePrefab;
        [SerializeField] private Transform _spherePosition;
        [SerializeField] private GameView _gameView;
        [SerializeField] private Ceiling _alertCeiling;
        [SerializeField] private Ceiling _gameOverCeiling;

        public int pastBestScore;
        [ReadOnly]
        public Sphere currentSphere;

        [ReadOnly]
        public Sphere nextSphere;

        [Header("Partycles")]
        public GameObject mergefX;
        public Transform fxSpawnPosition;

        [Header("Parameters")]
        [SerializeField, Range(0, 1.0f)] private float _audioVolume;

        //private GameModel _gameModel = new GameModel();
        public float respawnDelay;
        public AudioSource _audioSourceEffect;

        private ReactiveProperty<GameState> _reactiveGameState;
        public IReadOnlyReactiveProperty<GameState> gameState => _reactiveGameState;

        private ReactiveProperty<int> _reactiveCurrentScore;

        public ReactiveCommand<GameState> GameEvent = new ReactiveCommand<GameState>();

        public static GameManager Instance { get; private set; }
        public bool IsNext { get; set; }
        public int MaxSphereNo { get; private set; }

        private bool canDropItem;
        bool onlyOnceGameOver;

        int restoreSpheresData;

        public int nextSphereNo;

        [Space]
        [Header("Tutorial")]
        [SerializeField] private TutorialHandler _tutorial;

        private void OnEnable()
        {
            YG2.onRewardAdv += OnReward;
        }

        private void OnDisable()
        {
            YG2.onRewardAdv -= OnReward;
            CancelInvoke(nameof(SaveGameData));
        }

        private void OnReward(string id)
        {
            if (id == "continue_game_over_popup")
            {
                Revive();
            }
            else if (id == "unlock_big_item_popup")
            {
                UnlockedBigItemPopUpVisibleState(false);
                CreateCustomSphere(unlockBigItemSphere);
            }
        }


        private void Start()
        {
            int defaultSkin = 0;
            selectedIndex = PlayerPrefs.GetInt(WindowSelectSkin.SELECTED_SKIN_KEY, defaultSkin);


            currentSphere = CreateNewSphere();
            nextSphere = PlayerPrefs.HasKey("restoreSpheresData") ? CreateNewSphere() : currentSphere;

            _reactiveGameState = new ReactiveProperty<GameState>(CurrentState.Value);
            _reactiveCurrentScore = new ReactiveProperty<int>(CurrentScore.Value);

            Instance = this;
            IsNext = false;

            //MaxSphereNo = _spherePrefab.Length;
            MaxSphereNo = 11;

            SetGameState(GameState.Initializing);

            // Abonați-vă la evenimentul de picare din MoveAxe
            FindObjectOfType<MoveAxe>().OnDropItem.Subscribe(_ =>
            {
                // La apelul funcției DropItem în MoveAxe, apelați DropCurrentItem în GameManager
                DropCurrentItem();
            }).AddTo(this);

            SubscribeToGameEvents();
            SubscribeToScoreChanges();
            //SetBestScore();
            CreateSphere();
            _tutorial.InitializeWindow(selectedIndex, spheresCategory);
            LoadBestScore();

            if (PlayerPrefs.GetInt("newGamevalue", 0) == 1)
            {
                PlayerPrefs.DeleteKey("restoreSpheresData");
                NewGame();
            }
            else
            {
                if (PlayerPrefs.HasKey("restoreSpheresData"))
                {
                    LoadDataFromPlayerPrefs();

                }
            }

            InitializeTopPanel();
            RestoreCurrentScore();


            InvokeRepeating(nameof(SaveGameData), 5f, 5f);
        }



        public void SaveGameData()
        {
            Debug.Log("[GameManager] Save game data");
            SaveBestScore();
            SaveDataToPlayerPrefs();
        }

        #region merge goal related
        private int _currentMergeGoal = 100;
        private void InitializeTopPanel()
        {
            bool showMergeGoal = true;
            _gameView.InitializeTopPanel(showMergeGoal);

            if (!showMergeGoal)
                return;

            int maxSphere = 0;
            if (savedSpheresData.Count > 0)
                maxSphere = savedSpheresData.Max(data => data.sphere.SphereNo);

            if (maxSphere >= 10)
            {
                _gameView.HideGoalPanel();
            }
            else
            {
                _currentMergeGoal = 6;
                if (maxSphere >= 6)
                    _currentMergeGoal = maxSphere + 1;

                _gameView.SetMergeGoalSprite(spheresCategory[selectedIndex].spheresCategory[_currentMergeGoal].sphere.itemSpriteRenderer.sprite);
            }
        }

        public void HandleMergeGoalPanel(int createdSphereNumber)
        {
            if (createdSphereNumber >= 10)
            {
                _gameView.HideGoalPanel();
            }
            else
            {
                int nextGoal = createdSphereNumber + 1;
                if (nextGoal > _currentMergeGoal)
                {
                    _currentMergeGoal = nextGoal;
                    _gameView.SetMergeGoalSprite(spheresCategory[selectedIndex].spheresCategory[nextGoal].sphere.itemSpriteRenderer.sprite);
                }
            }
        }
        #endregion

        [Button]
        public void NewGame()
        {
            PlayerPrefs.DeleteKey("newGamevalue");

            Debug.LogWarning("NEW GAME !");

            ClearAllItems();
            ClearDataItemsList();

            int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(currentSceneIndex);
        }


        /// <summary>
        /// The skin picker now lives in the same scene as the game, so applying a new skin
        /// means: persist the current board and score, then reload this single scene so every
        /// sphere is re-created from the newly selected category.
        /// </summary>
        public void ReloadWithSelectedSkin()
        {
            SaveGameData();
            PlayerPrefs.Save();

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>
        /// The run used to be re-entered from the menu scene, which always reset the score.
        /// With a single scene the board survives a reload, so the score has to survive with it.
        /// </summary>
        private void RestoreCurrentScore()
        {
            if (!PlayerPrefs.HasKey(CurrentScoreKey))
                return;

            CurrentScore.Value = PlayerPrefs.GetInt(CurrentScoreKey, 0);
            _reactiveCurrentScore.Value = CurrentScore.Value;
        }

        private void Update()
        {
            if (IsNext)
            {
                Invoke(nameof(CreateSphere), respawnDelay);

                //Debug.LogWarning("Create New Sphere");
                IsNext = false;
            }
        }

        public void ShowReward_ClearSmallItems()
        {
            Debug.Log("ClearSmallItems: Showing rewarded ad...");

            YG2.RewardedAdvShow("clear_small_items", () =>
            {
                Debug.Log("ClearSmallItems: Ad watched. Clearing small items...");
                ClearSmallItems();
            });
        }


        public void ClearSmallItems()
        {
            Sphere[] spheres = _spherePosition.GetComponentsInChildren<Sphere>();

            foreach (Sphere sphere in spheres)
            {
                if (sphere.isSmallItem)
                {
                    Debug.Log("Found Small Fruit: " + sphere.gameObject.name);
                    sphere.DestroyItem();
                }
            }
        }


        public void ClearAllItems()
        {
            // Parcurge toți copiii și îi distruge
            int childCount = _spherePosition.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                // Obține copilul la indexul i și îl distruge
                Transform child = _spherePosition.GetChild(i);
                DestroyImmediate(child.gameObject);
            }
        }

        #region Game Logic

        private void SubscribeToGameEvents()
        {
            GameEvent.Subscribe(state =>
            {
                _reactiveGameState.Value = state;
                SetGameState(state);
                switch (state)
                {
                    case GameState.Initializing:
                        break;

                    case GameState.SphereMoving:
                        break;

                    case GameState.SphereDropping:

                        drop_sfx.Play();

                        OnSphereDrop?.Invoke();

                        break;

                    case GameState.Merging:
                        merge_sfx.Play();

                        break;

                    case GameState.GameOver:
                        HandleGameOver();
                        break;
                }
            }).AddTo(this);
        }

        private void SubscribeToScoreChanges()
        {
            _reactiveCurrentScore.Subscribe(score =>
            {
                _gameView.UpdateCurrentScore(score);
            }).AddTo(this);
        }

        public void SetCurrentScore(int SphereNo)
        {
            CalcScore(SphereNo);
            _reactiveCurrentScore.Value = CurrentScore.Value;
        }

        private void CreateSphere()
        {
            GameEvent.Execute(GameState.SphereMoving);

            Sphere sphereIns = Instantiate(currentSphere, sphereSpawnPosition, sphereSpawnPosition);
            moveAxe.GetCurrentSphereScale(sphereIns);

            // Resetăm poziția locală și rotația obiectului instantiat
            sphereIns.transform.localPosition = Vector3.zero;
            sphereIns.transform.localRotation = Quaternion.identity;

            currentSphere = sphereIns;
            canDropItem = true;

            _gameView.SetCurrentItemSprite(nextSphere.itemSpriteRenderer.sprite);
        }


        public void CreateCustomSphere(Sphere sphere)
        {
            ClearSpheresFromSphereSpawn();

            currentSphere = sphere;
            CreateSphere();
        }

        public void ClearSpheresFromSphereSpawn()
        {
            // Dacă există o referință la currentSphere
            if (currentSphere != null)
            {
                // Ștergeți imediat obiectul și eliminați-l din ierarhia scenei
                DestroyImmediate(currentSphere.gameObject);
                // Setați și referința la currentSphere ca null
                currentSphere = null;
            }
        }

        public Sphere CreateNewSphere()
        {
            GameEvent.Execute(GameState.SphereMoving);

            int maxIndex;
            if (selectedIndex >= 0 && selectedIndex < spheresCategory.Count)
            {
                maxIndex = spheresCategory[selectedIndex].spheresCategory.Count / 2 - 1;
            }
            else
            {
                Debug.LogError("Invalid selectedIndex. Please make sure it is within the range of available categories.");
                return null;
            }

            int i = UnityEngine.Random.Range(0, maxIndex + 1);
            Sphere sphereIns = spheresCategory[selectedIndex].spheresCategory[i].sphere;

            sphereIns.SphereNo = i;
            sphereIns.gameObject.SetActive(true);
            canDropItem = true;

            return sphereIns;
        }

        public void DropCurrentItem()
        {
            if (currentSphere && canDropItem)
            {
                GameEvent.Execute(GameState.SphereDropping);

                currentSphere.SphereRigidBodyGravity(true);
                //currentSphere._rb.simulated = true;
                IsNext = true;

                DeatachCurrentSphere(currentSphere);

                // Verifică dacă avem o sferă generată pentru a deveni următoarea sferă
                if (nextSphere != null)
                {
                    currentSphere = nextSphere;
                    nextSphere.gameObject.SetActive(true); // Activăm sfera generată anterior
                    nextSphere = null; // Resetăm nextSphere pentru a evita confuzii
                }
                else
                {
                    // Dacă nu avem o sferă generată, creează una nouă
                    CreateSphere();
                }

                // Setează nextSphere să fie o nouă sferă generată
                nextSphere = CreateNewSphere();

                canDropItem = false;
            }
        }

        public void DeatachCurrentSphere(Sphere _currentSphere)
        {
            // Deatașează currentSphere de sphereSpawnPosition
            _currentSphere.transform.parent = _spherePosition;
        }


        public void MergeNext(Vector3 target, int SphereNo)
        {
            if (selectedIndex >= 0 && selectedIndex < spheresCategory.Count && SphereNo < spheresCategory[selectedIndex].spheresCategory.Count - 1)
            {
                nextSphereNo = SphereNo + 1;
                Sphere sphereIns = Instantiate(spheresCategory[selectedIndex].spheresCategory[nextSphereNo].sphere, target, Quaternion.identity, _spherePosition);
                sphereIns.SphereNo = nextSphereNo;
                sphereIns.SphereRigidBodyGravity(true); ;
                sphereIns.gameObject.SetActive(true);

                if (sphereIns.isBigItem == true)
                {
                    if (!CheckIfBigItemIsCreated(sphereIns.itemName) == true)
                    {
                        UnlockedBigItemPopUpVisibleState(true);

                        unlockBigItemPopUp.BigItemPopUpSetData(sphereIns.itemName, sphereIns.itemSpriteRenderer.sprite);
                        unlockBigItemSphere = sphereIns;

                        bigItemsUnlocked.Add(sphereIns.itemName);
                    }
                }

                if (nextSphereNo == 10)
                {
                    unlockBigItemPopUp.UnlockState(1);

                    if (!PlayerPrefs.HasKey("unlockedLastItem"))
                        unlockBigItemPopUp.okButton.GetComponent<Button>().onClick.AddListener(UnlockedNewSkin);

                    PlayerPrefs.SetInt("unlockedLastItem", 1);
                }

                comboSystem.IncreaseComboCount();

                SetCurrentScore(SphereNo);

                // Instantiați mergefX și setați-i scala
                GameObject score3DText = Instantiate(score3DTextPrefab, target, Quaternion.identity, fxSpawnPosition);

                target += new Vector3(0, 0, 2f); // Adăugăm +1 la coordonata Z

                int scoreToAdd = (SphereNo + 1) * s_scoreCoefficient;
                CurrentScore.Value += scoreToAdd;

                score3DText.GetComponent<FxText>().CFXRCustomText(scoreToAdd, sphereIns.GetSphereColor());

                // Obține scala lui sphereIns
                Vector3 sphereScale = sphereIns.transform.localScale;

                // Instantiați mergefX și setați-i scala
                GameObject mergeFXInstance = Instantiate(mergefX, target, Quaternion.identity, fxSpawnPosition);
                mergeFXInstance.transform.position += new Vector3(0, 0, -1.9f); // Adăugăm +1 la coordonata Z

                mergeFXInstance.transform.localScale = sphereScale -= new Vector3(0.2f, 0.2f, 0.2f);

                HandleMergeGoalPanel(nextSphereNo);
            }
        }

        public void UnlockedNewSkin()
        {
            int unlockedSkin = selectedIndex == 0 ? 1 : 0;
            skinUnlocked.Show(spheresCategory[unlockedSkin].spheresCategory);
        }

        public bool CheckIfBigItemIsCreated(string itemName)
        {
            // Verificăm dacă valoarea string se găsește în lista bigItemsUnlocked
            return bigItemsUnlocked.Contains(itemName);
        }

        private void HandleGameOver()
        {
            if (!onlyOnceGameOver)
            {
                Debug.Log("GAME OVER!");

                SaveBestScore();
                gameOverPanel.SetActive(true);
                onlyOnceGameOver = true;
            }
        }

        public void ShowRewarded_ContinueAfterGameOver()
        {
            Debug.Log("GameOverPopup: Showing Yandex rewarded ad...");

            // Afișează reclama și dă recompensa dacă a fost vizionată complet
            YG2.RewardedAdvShow("continue_game_over_popup", () =>
            {
                Debug.Log("GameOverPopup: Ad watched. Reviving...");
                Revive();
            });
        }



        private void OnRewardVideo(string id)
        {
            if (id == "unlock_big_item_popup")
            {
                Debug.Log("UBIP: User watched ad, give reward.");
                UnlockedBigItemPopUpVisibleState(false);
                CreateCustomSphere(unlockBigItemSphere);
            }
            else if (id == "continue_game_over_popup")
            {
                Debug.Log("GameOverPopup: User watched ad, reviving player.");
                Revive();
            }
        }


        public void Revive()
        {
            ClearSmallItems();
            _gameOverCeiling.GetInvulnerability();
            gameOverPanel.SetActive(false);
            onlyOnceGameOver = false;
        }

        public void LoadAllSpheresFromScene()
        {
            // Găsește toate componente de tip Sphere în ierarhia _spherePosition
            Sphere[] foundSpheres = _spherePosition.GetComponentsInChildren<Sphere>();

            // Adaugă toate elementele găsite în lista curentSpheresList
            foreach (var foundSphere in foundSpheres)
            {
                SaveData missionData = new SaveData
                {
                    sphere = foundSphere,
                    itemID = foundSphere.itemID,
                    itemPosition = foundSphere.currentItemPosition

                    // Alte informații legate de salvare, dacă este cazul
                };

                savedSpheresData.Add(missionData);
            }

            // Afișează în consolă numărul de elemente adăugate
            //Debug.Log("Loaded " + foundSpheres.Length + " spheres from the scene.");
        }

        bool onlyOanceRestore;

        public void RestoreSphereData()
        {
            //Debug.Log("RestoreSphereData");

            if (!onlyOanceRestore)
            {
                foreach (var savedData in savedSpheresData)
                {
                    // Verificăm dacă datele salvate sunt valide
                    if (savedData != null && savedData.sphere != null)
                    {
                        // Încarcăm și poziția și rotația sferelor
                        Vector3 itemPosition = savedData.itemPosition;
                        Quaternion itemRotation = savedData.itemRotation;

                        // Adăugați poziția parintelui pentru a obține poziția globală
                        itemPosition += _spherePosition.position;

                        // Instantiați sfera
                        Sphere newSphere = Instantiate(savedData.sphere, itemPosition, itemRotation);

                        // Asigurăm că sfera este activată
                        newSphere.gameObject.SetActive(true);

                        // Setăm parentul sferii în ierarhia corectă, dacă este necesar
                        newSphere.transform.SetParent(_spherePosition);

                        newSphere.SphereRigidBodyGravity(true);
                        // Alte acțiuni necesare, dacă există
                    }
                    else
                    {
                        Debug.LogWarning("Datele salvate sau sfera asociată sunt nule.");
                    }
                }

                onlyOanceRestore = true;
            }
        }

        [Button]
        public int FindItemIndexByID(string id)
        {
            // Parcurgem toate categoriile
            for (int i = 0; i < spheresCategory.Count; i++)
            {
                // Parcurgem toate sferele din categoria curentă
                for (int j = 0; j < spheresCategory[i].spheresCategory.Count; j++)
                {
                    // Accesăm componenta GenerateID a sferei curente
                    GenerateID generateID = spheresCategory[i].spheresCategory[j].sphere.GetComponent<GenerateID>();

                    // Verificăm dacă ID-ul sferei curente corespunde cu ID-ul căutat
                    if (generateID != null && generateID.uniqueID == id)
                    {
                        // Returnăm indexul sferei găsite
                        return j;
                    }
                }
            }

            // Dacă nu găsim nicio corespondență, returnăm -1 pentru a indica că elementul nu a fost găsit
            return -1;
        }

        #endregion

        #region Unlock Big Item

        public void UnlockedBigItemPopUpVisibleState(bool value)
        {
            unlockBigItemPopUpGameobject.SetActive(value);
        }

        public void ShowReward_UnlockedBigItemPopUp()
        {
            Debug.Log("UBIP: Showing Yandex rewarded ad...");

            YG2.RewardedAdvShow("unlock_big_item_popup", () =>
            {
                Debug.Log("UBIP: Ad watched. Unlocking item...");
                UnlockedBigItemPopUpVisibleState(false);
                CreateCustomSphere(unlockBigItemSphere);
            });
        }


        public void FreeCreateCustomSphere()
        {
            CreateCustomSphere(unlockBigItemSphere);
        }

        #endregion

        #region Save Game Elements in scene

        [Button]
        public void ClearDataItemsList()
        {
            // Șterge cheile PlayerPrefs asociate datelor salvate din lista
            PlayerPrefs.DeleteKey("TotalItems");
            PlayerPrefs.DeleteKey(CurrentScoreKey);
            for (int i = 0; i < savedSpheresData.Count; i++)
            {
                PlayerPrefs.DeleteKey("ItemID_" + i);
                PlayerPrefs.DeleteKey("ItemPositionX_" + i);
                PlayerPrefs.DeleteKey("ItemPositionY_" + i);
                PlayerPrefs.DeleteKey("ItemPositionZ_" + i);
                PlayerPrefs.DeleteKey("ItemRotationX_" + i);
                PlayerPrefs.DeleteKey("ItemRotationY_" + i);
                PlayerPrefs.DeleteKey("ItemRotationZ_" + i);
                PlayerPrefs.DeleteKey("ItemRotationW_" + i);
                PlayerPrefs.DeleteKey("ItemIndexInList_" + i);
            }
            // Salvează ștergerea cheilor PlayerPrefs
            PlayerPrefs.Save();

            savedSpheresData.Clear();
        }

        [Button]
        public void SaveDataToPlayerPrefs()
        {
            ClearDataItemsList();

            Debug.LogWarning("SaveDataToPlayerPrefs");

            LoadAllSpheresFromScene();

            PlayerPrefs.SetInt("TotalItems", savedSpheresData.Count);

            int i = 0;
            foreach (SaveData missionData in savedSpheresData)
            {
                missionData.itemPosition = missionData.sphere.CurrentItemPosition();
                missionData.itemRotation = missionData.sphere.CurrentItemRotation();

                PlayerPrefs.SetString("ItemID_" + i, missionData.itemID);
                PlayerPrefs.SetFloat("ItemPositionX_" + i, missionData.itemPosition.x);
                PlayerPrefs.SetFloat("ItemPositionY_" + i, missionData.itemPosition.y);
                PlayerPrefs.SetFloat("ItemPositionZ_" + i, missionData.itemPosition.z);

                // Salvăm și rotația sferei
                PlayerPrefs.SetFloat("ItemRotationX_" + i, missionData.itemRotation.x);
                PlayerPrefs.SetFloat("ItemRotationY_" + i, missionData.itemRotation.y);
                PlayerPrefs.SetFloat("ItemRotationZ_" + i, missionData.itemRotation.z);
                PlayerPrefs.SetFloat("ItemRotationW_" + i, missionData.itemRotation.w);

                // Găsește indexul elementului în listă folosind ID-ul și setează itemIndexInList
                int index = FindItemIndexByID(missionData.itemID);
                missionData.itemIndexInList = index;

                PlayerPrefs.SetInt("ItemIndexInList_" + i, missionData.itemIndexInList);

                i++;
            }

            PlayerPrefs.Save();

            PlayerPrefs.SetInt(CurrentScoreKey, CurrentScore.Value);
            PlayerPrefs.SetInt("restoreSpheresData", restoreSpheresData);
        }

        [Button]
        public void LoadDataFromPlayerPrefs()
        {
            Debug.LogWarning("LoadDataFromPlayerPrefs");

            if (PlayerPrefs.HasKey("TotalItems"))
            {
                int totalItems = PlayerPrefs.GetInt("TotalItems");

                for (int i = 0; i < totalItems; i++)
                {
                    SaveData missionData = new SaveData();

                    missionData.itemID = PlayerPrefs.GetString("ItemID_" + i);
                    float posX = PlayerPrefs.GetFloat("ItemPositionX_" + i);
                    float posY = PlayerPrefs.GetFloat("ItemPositionY_" + i);
                    float posZ = PlayerPrefs.GetFloat("ItemPositionZ_" + i);

                    // Încarcăm și poziția și rotația sferelor
                    missionData.itemPosition = new Vector3(posX, posY, posZ);
                    float rotX = PlayerPrefs.GetFloat("ItemRotationX_" + i);
                    float rotY = PlayerPrefs.GetFloat("ItemRotationY_" + i);
                    float rotZ = PlayerPrefs.GetFloat("ItemRotationZ_" + i);
                    float rotW = PlayerPrefs.GetFloat("ItemRotationW_" + i);
                    missionData.itemRotation = new Quaternion(rotX, rotY, rotZ, rotW);

                    // Încărcați indexul elementului în listă și setați itemIndexInList
                    int index = PlayerPrefs.GetInt("ItemIndexInList_" + i);
                    missionData.itemIndexInList = index;

                    // Obțineți sfera corespunzătoare din lista de sfere folosind indexul
                    if (index >= 0 && index < spheresCategory[selectedIndex].spheresCategory.Count)
                    {
                        missionData.sphere = spheresCategory[selectedIndex].spheresCategory[index].sphere;
                    }
                    else
                    {
                        Debug.LogError("Invalid itemIndexInList: " + index);
                    }

                    savedSpheresData.Add(missionData);
                }
            }

            Invoke(nameof(RestoreSphereData), 0.2f);
        }

        #endregion

        #region Load All Category

        [ValueDropdown("GetCategoryNames"), OnValueChanged("RefreshList")]
        public string categoryName;

        [ReadOnly]
        public int selectedIndex;

        private IEnumerable<string> GetCategoryNames()
        {
            return this?.spheresCategory.ConvertAll(category => category.sphereCategoryName) ?? new List<string>();
        }

        private void RefreshList()
        {
            selectedIndex = this?.spheresCategory.FindIndex(category => category.sphereCategoryName == categoryName) ?? -1;
        }


        #endregion

        #region Clases

        [Serializable]
        public class SaveData
        {
            public Sphere sphere;
            [Space]
            public string itemID;
            public int itemIndexInList;
            [Space]
            public Vector3 itemPosition;
            public Quaternion itemRotation;
        }


        [Serializable]
        public class SpheresCategory
        {
            public string sphereCategoryName;
            public bool isLocked;

            public List<Spheres> spheresCategory = new List<Spheres>();
        }

        [Serializable]
        public class Spheres
        {
            public Sphere sphere;
        }

        #endregion

        #region Game Model

        public enum GameState
        {
            Initializing,
            SphereMoving,
            SphereDropping,
            Merging,
            GameOver
        }

        public enum SoundEffect
        {
            Drop,
            Merge
        }

        private ReactiveProperty<GameState> _currentState = new ReactiveProperty<GameState>(GameState.Initializing);
        private ReactiveProperty<int> _currentScore = new ReactiveProperty<int>(0);
        private ReactiveProperty<int> _bestScore = new ReactiveProperty<int>(0);

        private Dictionary<SoundEffect, AudioClip> soundEffects = new Dictionary<SoundEffect, AudioClip>();

        private static readonly int s_scoreCoefficient = 10;

        // the board is restored across a single-scene reload, so the running score is too
        private const string CurrentScoreKey = "CurrentScore";


        public ReactiveProperty<GameState> CurrentState
        {
            get { return _currentState; }
            private set { _currentState = value; }
        }

        public ReactiveProperty<int> CurrentScore
        {
            get { return _currentScore; }
            private set { _currentScore = value; }
        }

        public ReactiveProperty<int> BestScore
        {
            get { return _bestScore; }
            private set { _bestScore = value; }
        }

        public void SetGameState(GameState newState)
        {
            CurrentState.Value = newState;
        }

        #region Score

        public void CalcScore(int sphereNo)
        {
            int scoreToAdd = (sphereNo + 1) * s_scoreCoefficient;
            CurrentScore.Value += scoreToAdd;
        }

        public void SaveBestScore()
        {
            // Verificăm dacă scorul curent este mai mare decât scorul maxim anterior
            if (CurrentScore.Value > LoadBestScore())
            {
                // Salvăm noul scor maxim
                BestScore.Value = CurrentScore.Value;
                PlayerPrefs.SetInt("BestScore", BestScore.Value);
                PlayerPrefs.Save();

                _gameView.UpdateBestScore(BestScore.Value);

                //Debug.LogWarning("Save New best score value");
            }
            else
            {
                //Debug.LogWarning("No record !");
            }
        }


        [Button]
        public int LoadBestScore()
        {
            int bestScore = PlayerPrefs.GetInt("BestScore", BestScore.Value);

            _gameView.UpdateBestScore(bestScore);
            //Debug.LogError("Loaded Best Score : " + bestScore);
            return bestScore;
        }

        #endregion

        #endregion Game Model
        private void OnApplicationQuit()
        {
            SaveGameData();
        }
    }
}