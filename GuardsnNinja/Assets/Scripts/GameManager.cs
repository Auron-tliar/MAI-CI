using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

// The game conroller
public class GameManager : MonoBehaviour
{
    [HideInInspector]
    public Map MapGrid;
    
    public enum GameStages {Settings, RouteGeneration, PositionSelection, Simulation, End }
    public enum FitnessFunctions { DetectionBased, IntersectionBased, IntersectionScheme}
    [HideInInspector]
    public GameStages GameStage;

    public bool ShowGameSettingsMenu = true;

    [Header("Route generation parameters")]
    public bool ReadMapFromFile = false;
    public string MapFilePath;

    public int MapWidth = 10;
    public int MapHeight = 10;
    public int NumberOfGuards = 5;

    public int RouteGenotypeLength = 10;
    public int CrossoverPerStage = 2;

    public int DetectionRadius = 10;
    public int DetectionDecayTime = 10;

    public FitnessFunctions FitnessFunction = FitnessFunctions.IntersectionScheme;
    public int NumberOfSchemes = 20;
    public bool SimpleCrossover = true;
    public bool UseMinFunction = true;

    [Range(0,1)]
    public float MutationProbability = 0.01f;

    [Range(0, 1)]
    public float RouteMutationProbability = 0.5f;

    public int NumberOfIterations = 100;

    public int FitnessBinsX = 5;
    public int FitnessBinsY = 3;

    public int RandomSeed = -1;

    public Text FitnessDisplayField;


    [Header("Simulation parameters")]

    public float TurnTime = 5f;
    public int TurnLimit = 30;
    public float EnvironmentalNoiseProbability = 0.001f;
    public int EnvironmentalNoiseDegree = 10;
    public float CrawlNoise = 0.1f;
    public float WalkNoise = 0.5f;
    public float RunNoise = 1f;
    public float NoiseRandomRange = 0.2f;

    public float MinOutputToCheck = 5f;
    public float MinOutputForAlarm = 8f;

    public float NoiseDisplayTime = 2f;

    public int RandomNinjaNumber = 10;

    public Text TimerField;
    public GameObject SelectCellText;
    public Text EndText;
    public Text TurnsLeft;
    public GameObject RestartButton;
    public GameObject ReGenerateStatisticsButton;
    public GameObject StunLayer;


    [Header("Prefabs and Game Objects")]

    public GameObject MapTilePrefab;
    public GameObject GuardPrefab;
    public GameObject NinjaPrefab;

    public Transform MapContainer;
    public Transform GuardContainer;

    public Canvas SettingsCanvas;
    public Canvas RouteCanvas;
    public Canvas SimulationCanvas;


    [Header("Settings objects")]
    public Button LoadSettingsButton;

    public Toggle ReadFromFileField;
    public InputField MapFilePathField;
    public InputField MapWidthField;
    public InputField MapHeightField;
    public InputField NumberOfGuardsField;
    public InputField GenotypeLengthField;
    public InputField CrossoverNumberField;
    public InputField DetectionRadiusField;
    public InputField DetectionDecayField;
    public Dropdown FitnessFunctionField;
    public InputField NumberOfSchemesField;
    public Toggle SimpleCrossoverField;
    public Toggle UseMinFunctionField;
    public InputField MutationProbabilityField;
    public InputField RouteMutationProbabilityField;
    public InputField NumberOfIterationsField;
    public InputField FitnessBinsXField;
    public InputField FitnessBinsYField;
    public InputField RandomSeedField;

    public InputField TurnTimeField;
    public InputField TurnLimitField;
    public InputField EnvNoiseProbabilityField;
    public InputField EnvNoiseDegreeField;
    public InputField CrawlNoiseField;
    public InputField WalkNoiseField;
    public InputField RunNoiseField;
    public InputField NoiseRadiusField;
    public InputField MinOutputCheckField;
    public InputField MinOutputAlarmField;
    public InputField RandomNinjaNumberField;


    // Delegates
    public delegate void MoveTime(Map map, List<Noise> noises, Vector3 ninjaPos);
    public static event MoveTime OnMoveTime;

    public delegate void RandomMoveTime(Map map, List<Noise> noises, List<Vector2> ninjaPos);
    public static event RandomMoveTime OnRandomMoveTime;


    private Camera _mainCamera;

    private Vector3 _pointerPos;

    private float _timeLeft;
    private int _turnsLeft;

    private Noise _ninjaNoise;

    private bool _guardsStunned = false;

    private bool _randomNinjaMode = false;
    private List<Noise> _randomNinjaNoises;

    private GameObject _ninjaObject;

    private List<GuardController> _guardControllers;

    private List<Vector2> _randomNinjas;
    private HashSet<int> _crawlingNinjas;

    private int _correctAlarm;
    private int _incorrectAlarm;

    private StreamWriter sw;

	// Use this for initialization
	private void Awake()
    {
        // set the stage to the setting menu
        GameStage = GameStages.Settings;

        // get the main camera
        _mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        // open the log file for the fuzzy system
        sw = new StreamWriter("Fuzzy Log.txt", true);

        // if we don't want the setting screens (using within unity setting instead)
        if (!ShowGameSettingsMenu)
        {
            // then change the canvas and go strait to the GA
            SettingsCanvas.gameObject.SetActive(false);
            RouteCanvas.gameObject.SetActive(true);

            GameStage = GameStages.RouteGeneration;

            if (RandomSeed != -1)
            {
                Random.InitState(RandomSeed);
            }
            GenerateMap();
            SetGuards();
        }
        else
        {
            // otherwise wait for the user to select settings
            SettingsCanvas.gameObject.SetActive(true);
            RouteCanvas.gameObject.SetActive(false);

            if (PlayerPrefs.HasKey("SavedSettings"))
            {
                LoadSettingsButton.interactable = true;
                LoadSettingsButtonClick();
            }
            else
            {
                LoadSettingsButton.interactable = false;
            }
        }
	}
	
	// Update is called once per frame
	private void Update ()
    {
        if (GameStage != GameStages.Settings)
        {
            // at stages when the map is generated if it is too wide, we can move it with a left mouse button
            if (Input.GetMouseButton(0))
            {
                _mainCamera.transform.position =
                    new Vector3(_mainCamera.transform.position.x - (Input.mousePosition.x - _pointerPos.x) / _mainCamera.orthographicSize / 2,
                    _mainCamera.transform.position.y - (Input.mousePosition.y - _pointerPos.y) / _mainCamera.orthographicSize / 2,
                    _mainCamera.transform.position.z);
            }
            _pointerPos = Input.mousePosition;
        }

        if (GameStage == GameStages.Simulation) // if it is game simulation
        {
            if (_randomNinjaMode)
            { // in case of statistical random ninjas mode just run the whole simulation as a cycle of ninjas and 
                // guards movements
                for (int i = 0; i < TurnLimit; i++)
                {
                    MoveRandomNinjas();
                    MoveGuards();
                }

                // write the statistics
                sw.WriteLine("Number of turns: " + TurnLimit);
                sw.WriteLine("Number of random ninjas: " + RandomNinjaNumber);
                sw.WriteLine("Check noise minimal output: " + MinOutputToCheck);
                sw.WriteLine("Raise alarm minimal output: " + MinOutputForAlarm);
                sw.WriteLine("Environmental noise probability: " + EnvironmentalNoiseProbability);
                sw.WriteLine("Environmental noise degree: " + EnvironmentalNoiseDegree);
                sw.WriteLine("Crawl noise: " + CrawlNoise);
                sw.WriteLine("Walk noise: " + WalkNoise);
                sw.WriteLine("Run noise: " + RunNoise);
                sw.WriteLine("Correct alarms: " + _correctAlarm);
                sw.WriteLine("Incorrect alarm: " + _incorrectAlarm);
                sw.WriteLine("--------------------------------------");
                sw.Flush();

                // run the endgame procedure
                EndGame("Simulation over");
            }
            else
            {
                //otherwise the normal game
                // count the time and wait for user input
                _timeLeft -= Time.deltaTime;

                if (Input.GetButtonDown("Horizontal") || Input.GetButtonDown("Vertical") || Input.GetButtonDown("Skip"))
                {
                    if (Input.GetAxisRaw("Horizontal") > 0)
                    {
                        if (Input.GetButton("Run"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.East, 3);
                        }
                        else if (Input.GetButton("Crawl"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.East, 1);
                        }
                        else
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.East, 2);
                        }
                    }
                    else if (Input.GetAxisRaw("Horizontal") < 0)
                    {
                        if (Input.GetButton("Run"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.West, 3);
                        }
                        else if (Input.GetButton("Crawl"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.West, 1);
                        }
                        else
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.West, 2);
                        }
                    }
                    else if (Input.GetAxisRaw("Vertical") > 0)
                    {
                        if (Input.GetButton("Run"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.North, 3);
                        }
                        else if (Input.GetButton("Crawl"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.North, 1);
                        }
                        else
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.North, 2);
                        }
                    }
                    else if (Input.GetAxisRaw("Vertical") < 0)
                    {
                        if (Input.GetButton("Run"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.South, 3);
                        }
                        else if (Input.GetButton("Crawl"))
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.South, 1);
                        }
                        else
                        {
                            _ninjaNoise = MoveNinja(Node.Dir.South, 2);
                        }
                    }

                    // if the is an input, finish the game turn and store the generated noise
                    _timeLeft = 0f;

                }

                if (_timeLeft <= 0)
                {
                    // if the turn is over, mark it, check whether no more turns left and if not - move the guards
                    _turnsLeft--;
                    TurnsLeft.text = _turnsLeft.ToString();
                    if (_turnsLeft == 0)
                    {
                        EndGame("You are late!");
                    }
                    MoveGuards();
                    _timeLeft = TurnTime;
                    // the default 0 noise from the ninja
                    _ninjaNoise = new Noise(new Vector2(_ninjaObject.transform.position.x, _ninjaObject.transform.position.y), 0f);

                }

                TimerField.text = _timeLeft.ToString("F4");
            }
        }
	}

    // start with the selected settings
    public void RunSettings()
    {
        if (ReadFromFileField.isOn && !File.Exists(MapFilePathField.text))
        {
            ColorBlock cb = MapFilePathField.colors;
            cb.normalColor = new Color(1f, 0.7f, 0.7f);
            MapFilePathField.colors = cb;
            return;
        }

        ReadMapFromFile = ReadFromFileField.isOn;
        MapFilePath =  MapFilePathField.text;
        MapWidth = int.Parse(MapWidthField.text);
        MapHeight = int.Parse(MapHeightField.text);
        NumberOfGuards = int.Parse(NumberOfGuardsField.text);
        RouteGenotypeLength = int.Parse(GenotypeLengthField.text);
        CrossoverPerStage = int.Parse(CrossoverNumberField.text);
        DetectionRadius = int.Parse(DetectionRadiusField.text);
        DetectionDecayTime = int.Parse(DetectionDecayField.text);
        FitnessFunction = (FitnessFunctions)(FitnessFunctionField.value);
        NumberOfSchemes = int.Parse(NumberOfSchemesField.text);
        SimpleCrossover = SimpleCrossoverField.isOn;
        UseMinFunction = UseMinFunctionField.isOn;
        MutationProbability = float.Parse(MutationProbabilityField.text);
        RouteMutationProbability = float.Parse(RouteMutationProbabilityField.text);
        NumberOfIterations = int.Parse(NumberOfIterationsField.text);
        FitnessBinsX = int.Parse(FitnessBinsXField.text);
        FitnessBinsY = int.Parse(FitnessBinsYField.text);
        RandomSeed = int.Parse(RandomSeedField.text);

        TurnTime = float.Parse(TurnTimeField.text);
        TurnLimit = int.Parse(TurnLimitField.text);
        EnvironmentalNoiseProbability = float.Parse(EnvNoiseProbabilityField.text);
        EnvironmentalNoiseDegree = int.Parse(EnvNoiseDegreeField.text);
        CrawlNoise = float.Parse(CrawlNoiseField.text);
        WalkNoise = float.Parse(WalkNoiseField.text);
        RunNoise = float.Parse(RunNoiseField.text);
        NoiseRandomRange = float.Parse(NoiseRadiusField.text);
        MinOutputToCheck = float.Parse(MinOutputCheckField.text);
        MinOutputForAlarm = float.Parse(MinOutputAlarmField.text);
        RandomNinjaNumber = int.Parse(RandomNinjaNumberField.text);


        SettingsCanvas.gameObject.SetActive(false);
        RouteCanvas.gameObject.SetActive(true);

        if (RandomSeed != -1)
        {
            Random.InitState(RandomSeed);
        }
        if (!GenerateMap())
        {
            return;
        }
        SetGuards();
        GameStage = GameStages.RouteGeneration;
    }

    // save settings
    public void SaveSettingsButton()
    {
        PlayerPrefs.SetInt("SavedSettings", 1);

        if (ReadFromFileField.isOn)
        {
            PlayerPrefs.SetInt("ReadFromFile", 1);
        }
        else
        {
            PlayerPrefs.SetInt("ReadFromFile", 0);
        }
        PlayerPrefs.SetString("MapFilePath", MapFilePathField.text);
        PlayerPrefs.SetInt("MapWidth", int.Parse(MapWidthField.text));
        PlayerPrefs.SetInt("MapHeight", int.Parse(MapHeightField.text));
        PlayerPrefs.SetInt("NumberOfGuards", int.Parse(NumberOfGuardsField.text));
        PlayerPrefs.SetInt("GenotypeLength", int.Parse(GenotypeLengthField.text));
        PlayerPrefs.SetInt("CrossoverNumber", int.Parse(CrossoverNumberField.text));
        PlayerPrefs.SetInt("DetectionRadius", int.Parse(DetectionRadiusField.text));
        PlayerPrefs.SetInt("DetectionDecay", int.Parse(DetectionDecayField.text));
        PlayerPrefs.SetInt("FitnessFunction", FitnessFunctionField.value);
        PlayerPrefs.SetInt("NumberOfSchemes", int.Parse(NumberOfSchemesField.text));
        if (SimpleCrossoverField.isOn)
        {
            PlayerPrefs.SetInt("SimpleCrossover", 1);
        }
        else
        {
            PlayerPrefs.SetInt("SimpleCrossover", 0);
        }
        if (UseMinFunctionField.isOn)
        {
            PlayerPrefs.SetInt("UseMinFunction", 1);
        }
        else
        {
            PlayerPrefs.SetInt("UseMinFunction", 0);
        }
        PlayerPrefs.SetFloat("MutationProbability", float.Parse(MutationProbabilityField.text));
        PlayerPrefs.SetFloat("RouteMutationProbability", float.Parse(RouteMutationProbabilityField.text));
        PlayerPrefs.SetInt("NumberOfIterations", int.Parse(NumberOfIterationsField.text));
        PlayerPrefs.SetInt("FitnessBinsX", int.Parse(FitnessBinsXField.text));
        PlayerPrefs.SetInt("FitnessBinsY", int.Parse(FitnessBinsYField.text));
        PlayerPrefs.SetInt("RandomSeed", int.Parse(RandomSeedField.text));

        PlayerPrefs.SetFloat("TurnTime", float.Parse(TurnTimeField.text));
        PlayerPrefs.SetInt("TurnLimit", int.Parse(TurnLimitField.text));
        PlayerPrefs.SetFloat("EnvNoiseProbability", float.Parse(EnvNoiseProbabilityField.text));
        PlayerPrefs.SetInt("EnvNoiseDegree", int.Parse(EnvNoiseDegreeField.text));
        PlayerPrefs.SetFloat("CrawlNoise", float.Parse(CrawlNoiseField.text));
        PlayerPrefs.SetFloat("WalkNoise", float.Parse(WalkNoiseField.text));
        PlayerPrefs.SetFloat("RunNoise", float.Parse(RunNoiseField.text));
        PlayerPrefs.SetFloat("NoiseRadius", float.Parse(NoiseRadiusField.text));
        PlayerPrefs.SetFloat("MinOutputCheck", float.Parse(MinOutputCheckField.text));
        PlayerPrefs.SetFloat("MinOutputAlarm", float.Parse(MinOutputAlarmField.text));
        PlayerPrefs.SetInt("RandomNinjaNumber", int.Parse(RandomNinjaNumberField.text));

        LoadSettingsButton.interactable = true;
    }

    // load settings
    public void LoadSettingsButtonClick()
    {
        if (PlayerPrefs.GetInt("ReadFromFile", 1) == 1)
        {
            ReadFromFileField.isOn = true;
        }
        else
        {
            ReadFromFileField.isOn = false;
        }
        MapFilePathField.text = PlayerPrefs.GetString("MapFilePath", MapFilePathField.text);
        MapWidthField.text = PlayerPrefs.GetInt("MapWidth", int.Parse(MapWidthField.text)).ToString();
        MapHeightField.text = PlayerPrefs.GetInt("MapHeight", int.Parse(MapHeightField.text)).ToString();
        NumberOfGuardsField.text = PlayerPrefs.GetInt("NumberOfGuards", int.Parse(NumberOfGuardsField.text)).ToString();
        GenotypeLengthField.text = PlayerPrefs.GetInt("GenotypeLength", int.Parse(GenotypeLengthField.text)).ToString();
        CrossoverNumberField.text = PlayerPrefs.GetInt("CrossoverNumber", int.Parse(CrossoverNumberField.text)).ToString();
        DetectionRadiusField.text = PlayerPrefs.GetInt("DetectionRadius", int.Parse(DetectionRadiusField.text)).ToString();
        DetectionDecayField.text = PlayerPrefs.GetInt("DetectionDecay", int.Parse(DetectionDecayField.text)).ToString();
        FitnessFunctionField.value = PlayerPrefs.GetInt("FitnessFunction", FitnessFunctionField.value);
        NumberOfSchemesField.text = PlayerPrefs.GetInt("NumberOfSchemes", int.Parse(NumberOfSchemesField.text)).ToString();
        if (PlayerPrefs.GetInt("SimpleCrossover", 1) == 1)
        {
            SimpleCrossoverField.isOn = true;
        }
        else
        {
            SimpleCrossoverField.isOn = false;
        }
        if (PlayerPrefs.GetInt("UseMinFunction", 1) == 1)
        {
            UseMinFunctionField.isOn = true;
        }
        else
        {
            UseMinFunctionField.isOn = false;
        }
        MutationProbabilityField.text = 
            PlayerPrefs.GetFloat("MutationProbability", float.Parse(MutationProbabilityField.text)).ToString();
        RouteMutationProbabilityField.text = 
            PlayerPrefs.GetFloat("RouteMutationProbability", float.Parse(RouteMutationProbabilityField.text)).ToString();
        NumberOfIterationsField.text = 
            PlayerPrefs.GetInt("NumberOfIterations", int.Parse(NumberOfIterationsField.text)).ToString();
        FitnessBinsXField.text = PlayerPrefs.GetInt("FitnessBinsX", int.Parse(FitnessBinsXField.text)).ToString();
        FitnessBinsYField.text = PlayerPrefs.GetInt("FitnessBinsY", int.Parse(FitnessBinsYField.text)).ToString();
        RandomSeedField.text = PlayerPrefs.GetInt("RandomSeed", int.Parse(RandomSeedField.text)).ToString();

        TurnTimeField.text = PlayerPrefs.GetFloat("TurnTime", float.Parse(TurnTimeField.text)).ToString();
        TurnLimitField.text = PlayerPrefs.GetInt("TurnLimit", int.Parse(TurnLimitField.text)).ToString();
        EnvNoiseProbabilityField.text = 
            PlayerPrefs.GetFloat("EnvNoiseProbability", float.Parse(EnvNoiseProbabilityField.text)).ToString();
        EnvNoiseDegreeField.text = PlayerPrefs.GetInt("EnvNoiseDegree", int.Parse(EnvNoiseDegreeField.text)).ToString();
        CrawlNoiseField.text = PlayerPrefs.GetFloat("CrawlNoise", float.Parse(CrawlNoiseField.text)).ToString();
        WalkNoiseField.text = PlayerPrefs.GetFloat("WalkNoise", float.Parse(WalkNoiseField.text)).ToString();
        RunNoiseField.text = PlayerPrefs.GetFloat("RunNoise", float.Parse(RunNoiseField.text)).ToString();
        NoiseRadiusField.text = PlayerPrefs.GetFloat("NoiseRadius", float.Parse(NoiseRadiusField.text)).ToString();
        MinOutputCheckField.text = PlayerPrefs.GetFloat("MinOutputCheck", float.Parse(MinOutputCheckField.text)).ToString();
        MinOutputAlarmField.text = PlayerPrefs.GetFloat("MinOutputAlarm", float.Parse(MinOutputAlarmField.text)).ToString();
        RandomNinjaNumberField.text =
            PlayerPrefs.GetInt("RandomNinjaNumber", int.Parse(RandomNinjaNumberField.text)).ToString();
    }

    // construct map from tiles based on the generated or loaded map
    private bool GenerateMap()
    {
        if (ReadMapFromFile)
        {
            try
            {
                MapGrid = new Map(MapFilePath, FitnessFunction);
                MapWidth = MapGrid.Width;
                MapHeight = MapGrid.Height;
            }
            catch(MapFileException ex)
            {
                ex.streamReader.Close();
                ColorBlock cbl = MapFilePathField.colors;
                cbl.normalColor = new Color(1f, 0.7f, 0.7f);
                MapFilePathField.colors = cbl;

                GameStage = GameStages.Settings;
                SettingsCanvas.gameObject.SetActive(true);
                RouteCanvas.gameObject.SetActive(false);

                return false;
            }
        }
        else
        {
            MapGrid = new Map(MapWidth, MapHeight, FitnessFunction);
        }

        GameObject tempObject;
        Node tempNode;
        MapTileController tempTile;
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                tempObject = Instantiate(MapTilePrefab, MapContainer);
                tempNode = MapGrid.GetNode(x, y);
                tempNode.Tile = tempObject;
                tempTile = tempObject.GetComponent<MapTileController>();
                for (int i = 0; i < 4; i++)
                {
                    tempTile.ModifyBorder((Node.Dir)(i), tempNode.Edges[(Node.Dir)(i)].IsPassable,
                        tempNode.Edges[(Node.Dir)(i)].IsPerceptible);
                }
                tempObject.transform.position = new Vector3(x, y, 0f);
            }
        }


        _mainCamera.orthographicSize = MapHeight / 2f + 1;
        _mainCamera.transform.position = new Vector3(MapWidth / 2f, MapHeight / 2f - 0.5f, _mainCamera.transform.position.z);

        CameraBoundaries cb = _mainCamera.GetComponent<CameraBoundaries>();
        
        cb.BottomBoundary = -1.5f;
        cb.LeftBoundary = -1.5f;
        cb.TopBoundary = MapHeight + 0.5f;
        cb.RightBoundary = MapWidth + 0.5f;

        cb.Reinitialize();

        return true;
    }

    // use GA to find the patrol route scheme and construct guards from them
    private void SetGuards()
    {
        GameObject tempGuard;
        Guard behavior;

        _guardControllers = new List<GuardController>();

        List<SimpleRoute> lr = new List<SimpleRoute>();

        switch (FitnessFunction)
        {
            case FitnessFunctions.DetectionBased:
                lr = RouteOptimizer.FindScheme(NumberOfGuards, CrossoverPerStage, MapGrid, NumberOfIterations,
                    RouteGenotypeLength, MutationProbability, DetectionRadius, DetectionDecayTime, FitnessBinsX, 
                    FitnessBinsY, RandomSeed);
                break;
            case FitnessFunctions.IntersectionBased:
                lr = RouteOptimizer2.FindScheme(NumberOfGuards, CrossoverPerStage, MapGrid, NumberOfIterations, 
                    RouteGenotypeLength, MutationProbability, DetectionRadius, DetectionDecayTime, FitnessBinsX, 
                    FitnessBinsY, RandomSeed);
                break;
            case FitnessFunctions.IntersectionScheme:
                lr = RouteOptimizer3.FindScheme(NumberOfSchemes, NumberOfGuards, CrossoverPerStage, SimpleCrossover, MapGrid,
                    NumberOfIterations, RouteGenotypeLength, MutationProbability, RouteMutationProbability, UseMinFunction,
                    DetectionRadius, DetectionDecayTime, RandomSeed);
                break;
        }
        Vector2 curNode, prevNode;
        Color color;

        float totalFitness = 0f;

        for (int i = 0; i < NumberOfGuards; i++)
        {
            totalFitness += lr[i].Fitness;
            tempGuard = Instantiate(GuardPrefab, GuardContainer);
            behavior = new Guard(lr[i]);
            tempGuard.transform.position = new Vector3(lr[i].StartNode.x, lr[i].StartNode.y, 0f);

            do
            {
                color = new Color(Random.value, Random.value, Random.value);
            }
            while (color.grayscale > 0.75);
            tempGuard.GetComponent<GuardController>().color = color;

            curNode = lr[i].StartNode;
            MapGrid.ModifyNodeText(curNode, i.ToString());

            for (int j = 0; j < lr[i].PatrolRoutePhen.Count; j++)
            {
                prevNode = curNode;
                curNode = MapGrid.Move(curNode, lr[i].PatrolRoutePhen[j].Direction);

                MapGrid.ModifyNodeText(curNode, i.ToString());
                MapGrid.ModifyRouteDisplay(prevNode, curNode, lr[i].PatrolRoutePhen[j].Direction, color);
            }

            tempGuard.GetComponent<GuardController>().Behavior = behavior;
            tempGuard.GetComponent<GuardController>().SetAlarmLimits(MinOutputToCheck, MinOutputForAlarm);
            _guardControllers.Add(tempGuard.GetComponent<GuardController>());
        }

        totalFitness /= NumberOfGuards;
        FitnessDisplayField.text += totalFitness.ToString();
    }

    // start the simulation
    public void StartSimulation()
    {
        GameStage = GameStages.PositionSelection;

        RouteCanvas.gameObject.SetActive(false);
        SimulationCanvas.gameObject.SetActive(true);
        MapGrid.SetDetections(NumberOfGuards);

        _timeLeft = TurnTime;
        _turnsLeft = TurnLimit;
        TimerField.text = _timeLeft.ToString("F4");
        TurnsLeft.text = _turnsLeft.ToString();
        MapGrid.SetGoal();

        MapGrid.ClearRoutes();

        for (int i = 0; i < _guardControllers.Count; i++)
        {
            _guardControllers[i].ResetGuard();
        }

        if (_randomNinjaMode)
        {
            GenerateRandomNinjas();
            GameStage = GameStages.Simulation;
        }
        else
        {
            MapGrid.SetStartingNodes(true);

            SelectCellText.SetActive(true);
        }
    }

    // respond to the start button click
    public void StartButtonClick()
    {
        _randomNinjaMode = false;
        StartSimulation();
    }

    // respond to generate statistics button click
    public void StartRandomNinjaMode()
    {
        _randomNinjaMode = true;
        _correctAlarm = 0;
        _incorrectAlarm = 0;
        StartSimulation();
    }

    // restart the simulation
    public void RestartSimulation()
    {
        EndText.gameObject.SetActive(false);
        RestartButton.SetActive(false);
        ReGenerateStatisticsButton.SetActive(false);

        for (int i = 0; i < _guardControllers.Count; i++)
        {
            _guardControllers[i].ResetGuard();
        }

        _timeLeft = TurnTime;
        _turnsLeft = TurnLimit;
        TimerField.text = _timeLeft.ToString("F4");
        TurnsLeft.text = _turnsLeft.ToString();

        Destroy(_ninjaObject);

        if (_randomNinjaMode)
        {
            GenerateRandomNinjas();
            GameStage = GameStages.Simulation;
        }
        else
        {
            MapGrid.SetStartingNodes(true);

            GameStage = GameStages.PositionSelection;
            SelectCellText.SetActive(true);
        }
    }

    // respond to play again button click
    public void RestartButtonClick()
    {
        _randomNinjaMode = false;

        RestartSimulation();
    }

    // respond to regenerate statistics button click
    public void RestartRandomNinjaMode()
    {
        _randomNinjaMode = true;
        _correctAlarm = 0;
        _incorrectAlarm = 0;

        RestartSimulation();
    }


    // for the map tiles to call when they are clicked
    public void SelectStartCell(MapTileController selection)
    {
        SelectCellText.SetActive(false);
        MapGrid.SetStartingNodes(false);

        _ninjaObject = Instantiate(NinjaPrefab, selection.transform.position, new Quaternion());
        _ninjaNoise = new Noise(new Vector2(_ninjaObject.transform.position.x, _ninjaObject.transform.position.y), 0f);

        GameStage = GameStages.Simulation;
    }

    // to generate random ninjas
    public void GenerateRandomNinjas()
    {
        _randomNinjas = new List<Vector2>();
        _randomNinjaNoises = new List<Noise>();
        _crawlingNinjas = new HashSet<int>();
        for (int i = 0; i < RandomNinjaNumber; i++)
        {
            _randomNinjas.Add(new Vector2(Random.Range(0, MapWidth), Random.Range(0, MapHeight)));
            _randomNinjaNoises.Add(new Noise(_randomNinjas[i], 0f));
        }
    }

    // to process the move command for the ninja from the player
    private Noise MoveNinja(Node.Dir direction, int speed)
    {
        if (speed == 1)
        {
            _ninjaNoise = new Noise(new Vector2(_ninjaObject.transform.position.x, _ninjaObject.transform.position.y),
                GenerateNinjaNoise(speed));
            MoveGuards();
        }
        
        Vector2 newPos = new Vector2(_ninjaObject.transform.position.x, _ninjaObject.transform.position.y);
        for (int i = 0; i < speed; i++)
        {
            newPos = MapGrid.Move(newPos, direction);
        }

        _ninjaObject.transform.position = new Vector3(newPos.x, newPos.y);

        if (MapGrid.IsAtGoal(newPos))
        {
            EndGame("You win!");
        }

        return new Noise(new Vector2(_ninjaObject.transform.position.x, _ninjaObject.transform.position.y),
            GenerateNinjaNoise(speed));
    }

    // to move random ninjas
    private void MoveRandomNinjas()
    {
        int speed;
        Node.Dir direction;
        for (int i = 0; i < RandomNinjaNumber; i++)
        {
            direction = (Node.Dir)(Random.Range(0, 5));
            if (_crawlingNinjas.Contains(i))
            {
                _crawlingNinjas.Remove(i);
                _randomNinjas[i] = MapGrid.Move(_randomNinjas[i], direction);
                _randomNinjaNoises[i] = new Noise(_randomNinjas[i], GenerateNinjaNoise(1));
            }
            else
            {
                speed = Random.Range(0, 4);

                if (speed == 1)
                {
                    _crawlingNinjas.Add(i);
                    _randomNinjaNoises[i] = new Noise(_randomNinjas[i], GenerateNinjaNoise(1));
                }
                else
                {
                    for (int s = 0; s < speed; s++)
                    {
                        _randomNinjas[i] = MapGrid.Move(_randomNinjas[i], direction);
                    }
                    _randomNinjaNoises[i] = new Noise(_randomNinjas[i], GenerateNinjaNoise(speed));
                }
            }
        }
    }

    // to call for the environmental noise generation and then raise an event for guards to process their moves and detections
    private void MoveGuards()
    {
        if (_randomNinjaMode)
        {
            if (OnRandomMoveTime != null)
            {
                OnRandomMoveTime(MapGrid, GenerateNoise(), _randomNinjas);
            }
        }
        else
        {
            if (_guardsStunned)
            {
                _guardsStunned = false;
                StunLayer.SetActive(false);
            }
            else if (OnMoveTime != null)
            {
                OnMoveTime(MapGrid, GenerateNoise(), _ninjaObject.transform.position);
            }
        }
    }

    // to stop the simulation and change the game stage
    public void EndGame(string endMessage)
    {
        GameStage = GameStages.End;
        RestartButton.SetActive(true);
        ReGenerateStatisticsButton.SetActive(true);
        EndText.gameObject.SetActive(true);
        EndText.text = endMessage;
    }

    // generate a random number for env noise from -0.5 to 0.5 by adding n uniform random variables
    public float DegreeRandom(int n = 30)
    {
        float val = 0f;

        for (int i = 0; i < n; i++)
        {
            val += Random.Range(-0.5f / n, 0.5f / n);
        }

        return Mathf.Abs(val);
    }

    //generate a set of env noises
    private List<Noise> GenerateNoise()
    {
        List<Noise> noises = new List<Noise>();
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                if (Random.value <= EnvironmentalNoiseProbability)
                {
                    noises.Add(new Noise(new Vector2((float)x, (float)y), DegreeRandom(EnvironmentalNoiseDegree)));
                }
            }
        }

        //Add ninja noise

        if (_randomNinjaMode)
        {
            noises.AddRange(_randomNinjaNoises);
        }
        else
        {
            noises.Add(_ninjaNoise);

            for (int i = 0; i < noises.Count; i++)
            {
                MapGrid.SetNoise(true, noises[i]);
                StartCoroutine(TurnOffNoise(noises[i], NoiseDisplayTime));
            }
        }

        return noises;
    }

    // generate a noise produced by the ninja
    private float GenerateNinjaNoise(int speed)
    {
        switch (speed)
        {
            case 1:
                return Mathf.Clamp01(CrawlNoise + Random.Range(-NoiseRandomRange, NoiseRandomRange) * CrawlNoise);
            case 2:
                return Mathf.Clamp01(WalkNoise + Random.Range(-NoiseRandomRange, NoiseRandomRange) * WalkNoise);
            case 3:
                return Mathf.Clamp01(RunNoise + Random.Range(-NoiseRandomRange, NoiseRandomRange) * RunNoise);
            default:
                return 0f;
        }
    }

    // stun the guards in case of the false alarm
    public void StunGuards()
    {
        _guardsStunned = true;
        StunLayer.SetActive(true);
    }

    // coroutine to hide the noises after a predefined amount of time
    public IEnumerator TurnOffNoise(Noise noise, float time)
    {
        yield return new WaitForSeconds(time);

        MapGrid.SetNoise(false, noise);
    }

    // to call by guards when they raised an alarm during the random ninjas simulation
    public void ReportAlarm(bool correct)
    {
        if (correct)
        {
            _correctAlarm++;
        }
        else
        {
            _incorrectAlarm++;
        }
    }

    // to close the log file for the fuzzy system
    public void OnApplicationQuit()
    {
        sw.Close();
    }
}
