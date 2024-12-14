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
    //CanBeCleanedFloor = 1,
    //CanBeCleanedDishware = 2,
    //CanBeCleanedGlass = 3,

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
    //CanBeHeatedCookware = 14,
    //CanHeatCookware = 15,
    //CanBeStoveTopCooked = 16,
    //CanStoveTopCook = 17,
    //CanBeMicrowaved = 18,
    //CanMicrowave = 19,
    CanBeCooked = 20,
    //CanToast = 21,
    //CanBeFilledWithCoffee = 22,
    //CanFillWithCoffee = 23,
    //CanBeWatered = 24,
    //CanWater = 25,
    //CanBeFilledWithSoap = 26, // might not use this, instead categorize all as CanBeFullOrEmpty -see below 28
    //CanFillWithSoap = 27,
    //CanBeFullOrEmpty = 28, // for things that can be emptied like toilet paper, paper towel, tissue box
    CanToggleOnOff = 29,
    //CanBeBedMade = 30,
    //CanBeMounted = 31,
    //CanMount = 32,
    //CanBeHungTowel = 33,
    //CanHangTowel = 34,
    //CanBeOnToiletPaperHolder = 35, // do not use, use object specific receptacle instead
    //CanHoldToiletPaper = 36, // do not use, use object specific receptacle instead
    //CanBeClogged = 37,
    //CanUnclog = 38,
    //CanBeOmelette = 39,
    //CanMakeOmelette = 40,
    //CanFlush = 41,
    //CanTurnOnTV = 42,

    //// Might not use this, as picking up paintings is not really feasible if we have to lift and carry it. They are just way too big....
    //// All Painting Mount Stuff Here
    //CanMountSmall = 43,
    //CanMountMedium = 44,
    //CanMountLarge = 45,
    //CanBeMountedSmall = 46,
    //CanBeMountedMedium = 47,
    //CanBeMountedLarge = 48,

    //// End Painting Mount Stuff

    //CanBeLitOnFire = 49,
    //CanLightOnFire = 50,
    //CanSeeThrough = 51,
    ObjectSpecificReceptacle = 52,
}


public enum SimObjType
{
    // undefined is always the first value
    Undefined,

    AluminumFoil,
    AlarmClock,
    Apple,
    AppleSliced,
    ArmChair,
    BasketBall,
    BaseballBat,
    Bathtub,
    BathtubBasin,
    Bed,
    Bench,
    BeverageContainer,
    Book,
    Boots,
    Bowl,
    BowlDirty,
    BowlFilled,
    Bread,
    BreadSliced,
    ButterKnife,
    Candle,
    Cart,
    CD,
    Chair,
    CleaningTool,
    Cloth,
    ClothesDryer,
    CoffeeTable,
    CoffeeMachine,
    Container, // for physics version - see Bottle
    ContainerFull, // not used in physics
    CounterTop,
    CreditCard,
    Curtains,
    Desk,
    DeskLamp,
    Desktop,
    DishSponge,
    DogBed,
    Drawer,
    Dumbbell,
    Egg,
    EggCracked,
    EggShell,
    Faucet,
    Floor,
    FloorLamp,
    Fridge,
    GarbageBag,
    GarbageCan,
    HandTowel,
    HousePlant,
    KeyChain,
    Kettle,
    Ladle,
    Lamp, // DO NOT USE: don't use this, use either FloorLamp or DeskLamp
    Lettuce,//生菜
    LettuceSliced,
    LightSwitch,
    Lighter,
    LivingSpace,
    Microwave,
    MiscTableObject, // DO NOT USE: not sure what this is, not used for physics
    Mirror,
    Mug,
    MugFilled, // not used in physics
    Newspaper,
    Omelette,//煎蛋
    Pan,
    PanLid,
    PaperTowel,
    Pen,
    PepperShaker,
    Pencil,
    Pillow,
    Plunger,
    Plate,
    Potato,
    RemoteControl,
    Safe,
    Shelf,
    ShelvingUnit,
    ShowerCurtain,
    ShowerDoor,
    ShowerGlass,
    ShowerHead,
    Sink,
    SinkBasin,
    SoapBar,
    SoapBottle,
    SoapContainer,
    SoapBottleFilled, // DO NOT USE: Soap bottle now just has two states
    Sofa,
    Spatula,
    SportsEquipment, // DO NOT USE: delineated into specific objects in physics - see Basketball etc
    Stool,
    StoveBurner,
    StoveKnob,
    TeddyBear,
    Television,
    Tissue,
    TissueBox,
    TissueBoxEmpty, // will be a state of TissuBox in physics
    Toilet,
    ToiletPaper,
    ToiletPaperHanger,
    ToiletPaperRoll, // DO NOT USE ANYMORE - ToiletPaper is now a single object that toggles states
    Towel,//毛巾
    VacuumCleaner,
    Vase,
    Wall,
    WallMirror, // maybe don't use this, just use 'Mirror' instead?
    WashingMachine,
    Watch,
    Window,
    WineBottle,
}

