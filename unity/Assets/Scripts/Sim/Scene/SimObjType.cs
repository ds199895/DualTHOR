using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[Serializable]
public enum SimObjPrimaryProperty : int // EACH SimObjPhysics MUST HAVE 1 Primary propert, no more no less
{
    // NEVER LEAVE UNDEFINED
    Undefined = 0,

    // PRIMARY PROPERTIES
    Static = 1,
    Moveable = 2,
    CanPickup = 3,

    // these are to identify walls, floor, ceiling - these are not currently being used but might be in the future once
    // all scenes have their walls/floor/ceiling meshes split apart correctly (thanks Eli!)
    //这些未来可能会用到
    //Wall = 4,
    //Floor = 5,
    //Ceiling = 6,

    // Objaverse, uncategorized
    //不知道有啥用
    //Objaverse = 7,
}

//[Serializable]
public enum SimObjSecondaryProperty : int // EACH SimObjPhysics can have any number of Secondary Properties
{
    // NEVER LEAVE UNDEFINED
    Undefined = 0,

    // CLEANABLE PROPERTIES - this property defines what objects can clean certain objects - we might not use this, stay posted
    CanBeCleanedFloor = 1,
    CanBeCleanedDishware = 2,
    CanBeCleanedGlass = 3,

    // OTHER SECONDARY PROPERTIES
    CanBeDirty = 4,
    CanBeFilled = 5,
    CanBeUsedUp = 6,
    Receptacle = 7,
    CanOpen = 8,
    CanBeSliced = 9,
    CanSlice = 10,
    CanBreak = 11,
    isHeatSource = 12, // this object can change temperature of other objects to hot
    isColdSource = 13, // this object can change temperature of other objects to cold
    CanBeHeatedCookware = 14,
    CanHeatCookware = 15,
    CanBeStoveTopCooked = 16,
    CanStoveTopCook = 17,
    CanBeMicrowaved = 18,
    CanMicrowave = 19,
    CanBeCooked = 20,
    CanToast = 21,
    CanBeFilledWithCoffee = 22,
    CanFillWithCoffee = 23,
    CanBeWatered = 24,
    CanWater = 25,
    CanBeFilledWithSoap = 26, // might not use this, instead categorize all as CanBeFullOrEmpty -see below 28
    CanFillWithSoap = 27,
    CanBeFullOrEmpty = 28, // for things that can be emptied like toilet paper, paper towel, tissue box
    CanToggleOnOff = 29,
    CanBeBedMade = 30,
    CanBeMounted = 31,
    CanMount = 32,
    CanBeHungTowel = 33,
    CanHangTowel = 34,
    CanBeOnToiletPaperHolder = 35, // do not use, use object specific receptacle instead
    CanHoldToiletPaper = 36, // do not use, use object specific receptacle instead
    CanBeClogged = 37,
    CanUnclog = 38,
    CanBeOmelette = 39,
    CanMakeOmelette = 40,
    CanFlush = 41,
    CanTurnOnTV = 42,

    // Might not use this, as picking up paintings is not really feasible if we have to lift and carry it. They are just way too big....
    // All Painting Mount Stuff Here
    CanMountSmall = 43,
    CanMountMedium = 44,
    CanMountLarge = 45,
    CanBeMountedSmall = 46,
    CanBeMountedMedium = 47,
    CanBeMountedLarge = 48,

    // End Painting Mount Stuff

    CanBeLitOnFire = 49,
    CanLightOnFire = 50,
    CanSeeThrough = 51,
    ObjectSpecificReceptacle = 52,
}


public enum SimObjType
{
    // undefined is always the first value
    Undefined = 0,

    // ADD NEW VALUES BELOW
    // DO NOT RE-ARRANGE OLDER VALUES
    Apple = 1,
    AppleSliced = 2,
    Tomato = 3,
    TomatoSliced = 4,
    Bread = 5,
    BreadSliced = 6,
    Sink = 7,
    Pot = 8,
    Pan = 9,
    Knife = 10,
    Fork = 11,
    Spoon = 12,
    Bowl = 13,
    Toaster = 14,
    CoffeeMachine = 15,
    Microwave = 16,
    StoveBurner = 17,
    Fridge = 18,
    Cabinet = 19,
    Egg = 20,
    Chair = 21,
    Lettuce = 22,
    Potato = 23,
    Mug = 24,
    Plate = 25,
    DiningTable = 26,
    CounterTop = 27,
    GarbageCan = 28,
    Omelette = 29,
    EggShell = 30,
    EggCracked = 31,
    StoveKnob = 32,
    Container = 33, // for physics version - see Bottle
    Cup = 34,
    ButterKnife = 35,
    PotatoSliced = 36,
    MugFilled = 37, // not used in physics
    BowlFilled = 38, // not used in physics
    Statue = 39,
    LettuceSliced = 40,
    ContainerFull = 41, // not used in physics
    BowlDirty = 42, // not used in physics
    Sandwich = 43, // will need to make new prefab for physics
    Television = 44,
    HousePlant = 45,
    TissueBox = 46,
    VacuumCleaner = 47,
    Painting = 48, // delineated sizes in physics
    WateringCan = 49,
    Laptop = 50,
    RemoteControl = 51,
    Box = 52,
    Newspaper = 53,
    TissueBoxEmpty = 54, // will be a state of TissuBox in physics
    PaintingHanger = 55, // delineated sizes in physics
    KeyChain = 56,
    Dirt = 57, // physics will use a different cleaning system entirely
    CellPhone = 58,
    CreditCard = 59,
    Cloth = 60,
    Candle = 61,
    Toilet = 62,
    Plunger = 63,
    Bathtub = 64,
    ToiletPaper = 65,
    ToiletPaperHanger = 66,
    SoapBottle = 67,
    SoapBottleFilled = 68, // DO NOT USE: Soap bottle now just has two states
    SoapBar = 69,
    ShowerDoor = 70,
    SprayBottle = 71,
    ScrubBrush = 72,
    ToiletPaperRoll = 73, // DO NOT USE ANYMORE - ToiletPaper is now a single object that toggles states
    Lamp = 74, // DO NOT USE: don't use this, use either FloorLamp or DeskLamp
    LightSwitch = 75,
    Bed = 76,
    Book = 77,
    AlarmClock = 78,
    SportsEquipment = 79, // DO NOT USE: delineated into specific objects in physics - see Basketball etc
    Pen = 80,
    Pencil = 81,
    Blinds = 82,
    Mirror = 83,
    TowelHolder = 84,
    Towel = 85,
    Watch = 86,
    MiscTableObject = 87, // DO NOT USE: not sure what this is, not used for physics

    ArmChair = 88,
    BaseballBat = 89,
    BasketBall = 90,
    Faucet = 91,
    Boots = 92,
    Bottle = 93,
    DishSponge = 94,
    Drawer = 95,
    FloorLamp = 96,
    Kettle = 97,
    LaundryHamper = 98,
    LaundryHamperLid = 99,
    Lighter = 100,
    Ottoman = 101,
    PaintingSmall = 102,
    PaintingMedium = 103,
    PaintingLarge = 104,
    PaintingHangerSmall = 105,
    PaintingHangerMedium = 106,
    PaintingHangerLarge = 107,
    PanLid = 108,
    PaperTowelRoll = 109,
    PepperShaker = 110,
    PotLid = 111,
    SaltShaker = 112,
    Safe = 113,
    SmallMirror = 114, // maybe don't use this, use just 'Mirror' instead
    Sofa = 115,
    SoapContainer = 116,
    Spatula = 117,
    TeddyBear = 118,
    TennisRacket = 119,
    Tissue = 120,
    Vase = 121,
    WallMirror = 122, // maybe don't use this, just use 'Mirror' instead?
    MassObjectSpawner = 123,
    MassScale = 124,
    Footstool = 125,
    Shelf = 126,
    Dresser = 127,
    Desk = 128,
    SideTable = 129,
    Pillow = 130,
    Bench = 131,
    Cart = 132, // bathroom cart on wheels
    ShowerGlass = 133,
    DeskLamp = 134,
    Window = 135,
    BathtubBasin = 136,
    SinkBasin = 137,
    CD = 138,
    Curtains = 139,
    Poster = 140,
    HandTowel = 141,
    HandTowelHolder = 142,
    Ladle = 143,
    WineBottle = 144,
    ShowerCurtain = 145,
    ShowerHead = 146,
    TVStand = 147,
    CoffeeTable = 148,
    ShelvingUnit = 149,
    AluminumFoil = 150,
    DogBed = 151,
    Dumbbell = 152,
    TableTopDecor = 153, // for display pieces that are meant to be decorative and placed on tables, shelves, in cabinets etc.
    RoomDecor = 154, // for display pieces that are mean to go on the floor of rooms, like the decorative sticks
    Stool = 155,
    GarbageBag = 156,
    Desktop = 157,
    TargetCircle = 158,
    Floor = 159,
    ScreenFrame = 160,
    ScreenSheet = 161,
    Wall = 162,
    Doorway = 163,
    WashingMachine = 164,
    ClothesDryer = 165,
    Doorframe = 166,

    Objaverse = 167
}

