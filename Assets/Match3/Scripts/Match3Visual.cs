using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CodeMonkey.Utils;

/*
 * Visual Representation of the underlying Match3 Grid
 * */
public class Match3Visual : MonoBehaviour {

    public event EventHandler OnStateChanged;
    public event EventHandler OnUserChanged;
    public enum State {
        Busy,
        WaitingForUser,
        TryFindMatches,
        GameOver,
    }
    public enum UserState
    {
        NONE,
        PLAYER,
        OPPONENT,
        BOT
        
       
    }

    [SerializeField] private Transform pfGemGridVisual;
    [SerializeField] private Transform pfGlassGridVisual;
    [SerializeField] private Transform pfBackgroundGridVisual;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Match3 match3;
    [SerializeField] private Timer timer;

    private Grid<Match3.GemGridPosition> grid;
    private Dictionary<Match3.GemGrid, GemGridVisual> gemGridDictionary;
    private Dictionary<Match3.GemGridPosition, GlassGridVisual> glassGridDictionary;

    private bool isSetup;
    private State state;
    public UserState userState;
    private float busyTimer;
    private Action onBusyTimerElapsedAction;

    private int startDragX;
    private int startDragY;
    private Vector3 startDragMouseWorldPosition;

    private void Awake() {
        state = State.Busy;
        isSetup = false;
        userState = UserState.NONE;
        match3.OnLevelSet += Match3_OnLevelSet;
       
    } 

    public void GenerateGrid()
    {

        FindObjectOfType<Match3>().LevelGridGenerate();
    }
    private void Match3Visual_OnUserChanged()
    {

    }
    private void Match3_OnLevelSet(object sender, Match3.OnLevelSetEventArgs e) {
        FunctionTimer.Create(() => {
            Setup(sender as Match3, e.grid);
        }, .1f);
    }

    public void Setup(Match3 match3, Grid<Match3.GemGridPosition> grid) {
        this.match3 = match3;
        this.grid = grid;

        float cameraYOffset = 1f;
        cameraTransform.position = new Vector3(grid.GetWidth() * .5f, grid.GetHeight() * .5f + cameraYOffset, cameraTransform.position.z);

        match3.OnGemGridPositionDestroyed += Match3_OnGemGridPositionDestroyed;
        match3.OnNewGemGridSpawned += Match3_OnNewGemGridSpawned;

        // Initialize Visual
        gemGridDictionary = new Dictionary<Match3.GemGrid, GemGridVisual>();
        glassGridDictionary = new Dictionary<Match3.GemGridPosition, GlassGridVisual>();

        for (int x = 0; x < grid.GetWidth(); x++) {
            for (int y = 0; y < grid.GetHeight(); y++) {
                Match3.GemGridPosition gemGridPosition = grid.GetGridObject(x, y);
                Match3.GemGrid gemGrid = gemGridPosition.GetGemGrid();

                Vector3 position = grid.GetWorldPosition(x, y);
                position = new Vector3(position.x, 12);

                // Visual Transform
                Transform gemGridVisualTransform = Instantiate(pfGemGridVisual, position, Quaternion.identity);
                gemGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>().sprite = gemGrid.GetGem().sprite;

                GemGridVisual gemGridVisual = new GemGridVisual(gemGridVisualTransform, gemGrid);

                gemGridDictionary[gemGrid] = gemGridVisual;

                // Glass Visual Transform
                Transform glassGridVisualTransform = Instantiate(pfGlassGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);

                GlassGridVisual glassGridVisual = new GlassGridVisual(glassGridVisualTransform, gemGridPosition);

                glassGridDictionary[gemGridPosition] = glassGridVisual;

                // Background Grid Visual
                Instantiate(pfBackgroundGridVisual, grid.GetWorldPosition(x, y), Quaternion.identity);
            }
        }

        SetBusyState(.5f, () => SetState(State.TryFindMatches));

        isSetup = true;
    }

    private void Match3_OnNewGemGridSpawned(object sender, Match3.OnNewGemGridSpawnedEventArgs e) {
        Vector3 position = e.gemGridPosition.GetWorldPosition();
        position = new Vector3(position.x, 12);

        Transform gemGridVisualTransform = Instantiate(pfGemGridVisual, position, Quaternion.identity);
        gemGridVisualTransform.Find("sprite").GetComponent<SpriteRenderer>().sprite = e.gemGrid.GetGem().sprite;

        GemGridVisual gemGridVisual = new GemGridVisual(gemGridVisualTransform, e.gemGrid);

        gemGridDictionary[e.gemGrid] = gemGridVisual;
    }

    private void Match3_OnGemGridPositionDestroyed(object sender, System.EventArgs e) {
        Match3.GemGridPosition gemGridPosition = sender as Match3.GemGridPosition;
        if (gemGridPosition != null && gemGridPosition.GetGemGrid() != null) {
            gemGridDictionary.Remove(gemGridPosition.GetGemGrid());
        }
    }

    private void Update() {
        if (!isSetup) return;

        UpdateVisual();

        switch (state) {
            case State.Busy:
                busyTimer -= Time.deltaTime;
                if (busyTimer <= 0f) {
                    onBusyTimerElapsedAction();
                }
                break;
            case State.WaitingForUser:
                if (Input.GetMouseButtonDown(0)) {
                    Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                    grid.GetXY(mouseWorldPosition, out startDragX, out startDragY);

                    startDragMouseWorldPosition = mouseWorldPosition;
                }

                if (Input.GetMouseButtonUp(0)) {
                    Vector3 mouseWorldPosition = UtilsClass.GetMouseWorldPosition();
                    grid.GetXY(mouseWorldPosition, out int x, out int y);

                    if (x != startDragX) {
                        // Different X
                        y = startDragY;

                        if (x < startDragX) {
                            x = startDragX - 1;
                        } else {
                            x = startDragX + 1;
                        }
                    } else {
                        // Different Y
                        x = startDragX;

                        if (y < startDragY) {
                            y = startDragY - 1;
                        } else {
                            y = startDragY + 1;
                        }
                    }

                    if (match3.CanSwapGridPositions(startDragX, startDragY, x, y)) {
                        SwapGridPositions(startDragX, startDragY, x, y);
                        Debug.Log("Grid Swip");
                    }
                }
                break;
            case State.TryFindMatches:
                if (match3.TryFindMatchesAndDestroyThem()) {
                    SetBusyState(.3f, () => {
                        match3.FallGemsIntoEmptyPositions();

                        SetBusyState(.3f, () => {
                            match3.SpawnNewMissingGridPositions();

                            SetBusyState(.5f, () => SetState(State.TryFindMatches));
                        });
                    });
                } else {
                    Debug.Log("WAITING FOR USER ");
                    TrySetStateWaitingForUser();
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void UpdateVisual() {
        foreach (Match3.GemGrid gemGrid in gemGridDictionary.Keys) {
            gemGridDictionary[gemGrid].Update();
        }
    }

    public void SwapGridPositions(int startX, int startY, int endX, int endY) {
        match3.SwapGridPositions(startX, startY, endX, endY);
        match3.UseMove();

        SetBusyState(.5f, () => SetState(State.TryFindMatches));
    }

    private void SetBusyState(float busyTimer, Action onBusyTimerElapsedAction) {
        SetState(State.Busy);
        this.busyTimer = busyTimer;
        this.onBusyTimerElapsedAction = onBusyTimerElapsedAction;
    }

    private void TrySetStateWaitingForUser() {

       
        if (match3.TryIsGameOver()) {
            // Game Over!
            Debug.Log("Game Over!");
            SetState(State.GameOver);
        } else {
            // Keep Playing
            timer.DisableTimer();
            SetState(State.WaitingForUser);
            switch (userState)
            {
                case UserState.NONE:

                   
                    timer.StopAllCoroutines();
                    timer.EnableTimer();
                    timer.Countdown(15, UserState.PLAYER);

                    SetUserState(UserState.PLAYER);
                    break;
                case UserState.PLAYER:

                    //float randomTime= UnityEngine.Random.Range(6,timer.timeRemaining);
                    ///
                    timer.timerIsRunning = false;
                    timer.StopAllCoroutines();
                    timer.EnableTimer();
                    
                    int nRandom = UnityEngine.Random.Range(5, 15);
                    timer.Countdown(nRandom,UserState.BOT);
                   // SetUserState(UserState.BOT);
                    break;
                case UserState.BOT:

                    timer.StopAllCoroutines();
                    timer.EnableTimer();
                    SetUserState(UserState.PLAYER);
                    timer.Countdown(15, UserState.PLAYER);
                    SetUserState(UserState.PLAYER);
                    break;
            }
        }
    }



    private void SetState(State state) {
        this.state = state;
        Debug.Log("Current State ..........." + this.state);
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }
    public void SetUserState(UserState state)
    {
        switch (state)
        {
            case UserState.PLAYER:

                match3.SetPlayer("YOU");
                break;
            case UserState.OPPONENT:
                match3.SetPlayer("OPPONENT");
                break;
            case UserState.BOT:
                match3.SetPlayer("BOT");
                break;
        }
        this.userState = state;
        OnUserChanged?.Invoke(this.userState, EventArgs.Empty);
    }

    private void SetPlayerTurnVisual()
    {

    }
    public State GetState() {
        return state;
    }
    public UserState GetUserState()
    {
        return userState;
    }
    public class GemGridVisual {

        private Transform transform;
        private Match3.GemGrid gemGrid;

        public GemGridVisual(Transform transform, Match3.GemGrid gemGrid) {
            this.transform = transform;
            this.gemGrid = gemGrid;

            gemGrid.OnDestroyed += GemGrid_OnDestroyed;
        }

        private void GemGrid_OnDestroyed(object sender, System.EventArgs e) {
            transform.GetComponent<Animation>().Play();
            Destroy(transform.gameObject, 1f);
        }

        public void Update() {
            Vector3 targetPosition = gemGrid.GetWorldPosition();
            Vector3 moveDir = (targetPosition - transform.position);
            float moveSpeed = 10f;
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }

    }

    public class GlassGridVisual {

        private Transform transform;
        private Match3.GemGridPosition gemGridPosition;

        public GlassGridVisual(Transform transform, Match3.GemGridPosition gemGridPosition) {
            this.transform = transform;
            this.gemGridPosition = gemGridPosition;

            transform.gameObject.SetActive(gemGridPosition.HasGlass());

            gemGridPosition.OnGlassDestroyed += GemGridPosition_OnGlassDestroyed;
        }

        private void GemGridPosition_OnGlassDestroyed(object sender, EventArgs e) {
            transform.gameObject.SetActive(gemGridPosition.HasGlass());
        }
    }

}
