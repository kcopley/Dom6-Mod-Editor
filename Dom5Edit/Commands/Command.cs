namespace Dom5Edit.Commands
{
    public enum Command
    {
        MODNAME,
        DESCRIPTION,
        ICON,
        VERSION,
        DOMVERSION,
        SELECTWEAPON,
        END,
        NAME,
        CLEAR,
        COPYWEAPON,
        DMG,
        NRATT,
        ATT,
        DEF,
        NEWWEAPON,
        LEN,
        TWOHANDED,
        SOUND,
        RANGE,
        AMMO,
        RCOST,
        SAMPLE,
        NATURAL,
        DT_NORMAL,
        DT_POISON,
        DT_DEMON,
        DT_SMALL,
        DT_MAGIC,
        DT_LARGE,
        DT_CONSTRUCTONLY,
        DT_RAISE,
        DT_CAP,
        DT_WEAKNESS,
        DT_HOLY,
        DT_DRAIN,
        DT_SIZESTUN,
        DT_WEAPONDRAIN,
        DT_STUN,
        DT_REALSTUN,
        DT_INTERRUPT,
        DT_BOUNCEKILL,
        DT_PARALYZE,
        DT_AFF,
        POISON,
        ACID,
        SLASH,
        PIERCE,
        BLUNT,
        COLD,
        FIRE,
        SHOCK,
        MAGIC,
        ARMORPIERCING,
        ARMORNEGATING,
        NOSTR,
        BOWSTR,
        HALFSTR,
        FULLSTR,
        MRNEGATES,
        MRNEGATESEASILY,
        HARDMRNEG,
        SIZERESIST,
        MIND,
        UNDEADIMMUNE,
        INANIMATEIMMUNE,
        FLYINGIMMUNE,
        ENEMYIMMUNE,
        FRIENDLYIMMUNE,
        UNDEADONLY,
        SACREDONLY,
        DEMONONLY,
        DEMONUNDEAD,
        INTERNAL,
        AOE,
        BONUS,
        SECONDARYEFFECT,
        SECONDARYEFFECTALWAYS,
        IRONWEAPON,
        WOODENWEAPON,
        ICEWEAPON,
        CHARGE,
        FLAIL,
        NOREPEL,
        UNREPEL,
        BEAM,
        RANGE050,
        RANGE0,
        MELEE50,
        SKIP,
        SKIP2,
        EXPLSPR,
        FLYSPR,
        UWOK,
        NOUW,
        SOULSLAYING,
        SELECTARMOR,
        NEWARMOR,
        COPYARMOR,
        TYPE,
        PROT,
        ENC,
        MAGICARMOR,
        IRONARMOR,
        WOODENARMOR,
        SELECTMONSTER,
        NEWMONSTER,
        FIXEDNAME,
        DESCR,
        SPR1,
        SPR2,
        SPECIALLOOK,
        DRAWSIZE,
        CLEARWEAPONS,
        CLEARARMOR,
        CLEARMAGIC,
        CLEARSPEC,
        COPYSTATS,
        COPYSPR,
        PATHCOST,
        STARTDOM,
        HOMEREALM,
        GCOST,
        TRIPLEGOD,
        TRIPLEGODMAG,
        UNIFY,
        TRIPLE3MON,
        MINPRISON,
        MAXPRISON,
        SLOWREC,
        NOSLOWREC,
        RECLIMIT,
        ENCHREBATE50,
        ENCHREBATE25P,
        ENCHREBATE50P,
        REQLAB,
        NOREQLAB,
        REQTEMPLE,
        NOREQTEMPLE,
        HEATREC,
        COLDREC,
        CHAOSREC,
        DEATHREC,
        AISINGLEREC,
        AINOREC,
        MONPRESENTREC,
        OWNSMONREC,
        DOMREC,
        SINGLEBATTLE,
        DESERTER,
        HORRORDESERTER,
        DEFECTOR,
        NOWISH,
        RPCOST,
        RESSIZE,
        HP,
        STR,
        PREC,
        SIZE,
        MR,
        MOR,
        MAPMOVE,
        AP,
        EYES,
        VOIDSANITY,
        WEAPON,
        ARMOR,
        HUMANOID,
        MOUNTEDHUMANOID,
        QUADRUPED,
        LIZARD,
        NAGA,
        SNAKE,
        BIRD,
        DJINN,
        TROGLODYTE,
        MISCSHAPE,
        STARTITEM,
        USERESTRICTEDITEM,
        NOITEM,
        ITEMSLOTS,
        FEMALE,
        COLDBLOOD,
        DRAKE,
        PLANT,
        LESSERHORROR,
        GREATERHORROR,
        DOOMHORROR,
        MOUNTED,
        HOLY,
        ANIMAL,
        UNIQUE,
        UNDEAD,
        BUG,
        DEMON,
        MAGICBEING,
        AUTOCOMPETE,
        BLIND,
        UWBUG,
        STONEBEING,
        INANIMATE,
        MINDLESS,
        DUNGEON,
        LANCEOK,
        IMMOBILE,
        AQUATIC,
        AMPHIBIAN,
        POORAMPHIBIAN,
        FLOAT,
        FLYING,
        SWIMMING,
        SNOW,
        STORMIMMUNE,
        TELEPORT,
        MAPTELEPORT,
        BLINK,
        UNTELEPORTABLE,
        NORIVERPASS,
        FORESTSURVIVAL,
        MOUNTAINSURVIVAL,
        SWAMPSURVIVAL,
        WASTESURVIVAL,
        SAILING,
        GIFTOFWATER,
        INDEPMOVE,
        INDEPSTAY,
        NORANGE,
        NOMOVEPEN,
        FARSAIL,
        ILLUSION,
        SEDUCE,
        SUCCUBUS,
        STEALTHY,
        SPY,
        ASSASSIN,
        PATIENCE,
        SCALEWALLS,
        BECKON,
        FALSEARMY,
        FOOLSCOUTS,
        STARTAGE,
        MAXAGE,
        OLDER,
        ADDRANDOMAGE,
        UWDAMAGE,
        HEAL,
        NOHEAL,
        LANDDAMAGE,
        HEALER,
        HOMESICK,
        AUTOHEALER,
        AUTODISHEALER,
        AUTODISGRINDER,
        DISEASERES,
        WOUNDFEND,
        HPOVERFLOW,
        HPOVERSLOW,
        CORPSEEATER,
        DEADHP,
        STARTAFF,
        STARTMAJORAFF,
        STARTINGAFF,
        INSANE,
        STARTHEROAB,
        BLUNTRES,
        ETHEREAL,
        COLDRES,
        FIRERES,
        POISONRES,
        SHOCKRES,
        ICEPROT,
        INVULNERABLE,
        REGENERATION,
        DOHEAL,
        UNDREGEN,
        UWREGEN,
        PIERCERES,
        SLASHRES,
        REINVIGORATION,
        AIRSHIELD,
        IRONVUL,
        IMMORTAL,
        DOMIMMORTAL,
        FORCESS,
        SPIKES,      // #spikes <dmg> (monster)
        SLEEPRES,    // #sleepres <bonus> (monster / item: Undreaming)
        REFORMTIME,
        SPRINGIMMORTAL,
        REFORM,
        BUGREFORM,
        HEAT,
        UWHEAT,
        POISONARMOR,
        POISONSKIN,
        POISONCLOUD,
        DISEASECLOUD,
        ANIMALAWE,
        AWE,
        SUNAWE,
        HALTHERETIC,
        FEAR,
        FIRESHIELD,
        UWFIRESHIELD,
        BANEFIRESHIELD,
        ACIDSHIELD,
        CURSELUCKSHIELD,
        DAMAGEREV,
        BLOODVENGEANCE,
        SLIMER,
        ENTANGLE,
        EYELOSS,
        HORRORMARK,
        MINDSLIME,
        OVERCHARGED,
        SLEEPAURA,
        SPRINGPOWER,
        SUMMERPOWER,
        FALLPOWER,
        WINTERPOWER,
        YEARTURN,
        CHAOSPOWER,
        COLDPOWER,
        FIREPOWER,
        DEATHPOWER,
        DARKPOWER,
        STORMPOWER,
        MAGICPOWER,
        SLOTHPOWER,
        DOMPOWER,
        AMBIDEXTROUS,
        BERSERK,
        BLESSBERS,
        BLESSFLY,
        DARKVISION,
        SPIRITSIGHT,
        INVISIBLE,
        DEATHCURSE,
        DEATHDISEASE,
        DEATHPARALYZE,
        DEATHFIRE,
        GUARDSPIRITBONUS,
        TRAMPSWALLOW,
        RAISEONKILL,
        TRAMPLE,
        DIGEST,
        ACIDDIGEST,
        INCORPORATE,
        RAISESHAPE,
        BEARTATTOO,
        HORSETATTOO,
        WOLFTATTOO,
        BOARTATTOO,
        SNAKETATTOO,
        CASTLEDEF,
        SIEGEBONUS,
        PATROLBONUS,
        PILLAGEBONUS,
        INQUISITOR,
        SUPPLYBONUS,
        HERETIC,
        RESOURCES,
        ICEFORGING,
        NEEDNOTEAT,
        ELEGIST,
        SPREADDOM,
        NOBADEVENTS,
        SHATTEREDSOUL,
        INCUNREST,
        INCPROVDEF,
        TAXCOLLECTOR,
        GOLD,
        ADDUPKEEP,
        LEPER,
        POPKILL,
        INSANIFY,
        NOHOF,
        ALCHEMY,
        MASON,
        INCSCALE,
        DECSCALE,
        FORTKILL,
        THRONEKILL,
        FARTHRONEKILL,
        LOCALSUN,
        ADEPTSACR,
        SHAPECHANGE,
        PROPHETSHAPE,
        FIRSTSHAPE,
        SECONDSHAPE,
        SECONDTMPSHAPE,
        FORESTSHAPE,
        PLAINSHAPE,
        FOREIGNSHAPE,
        HOMESHAPE,
        DOMSHAPE,
        NOTDOMSHAPE,
        SPRINGSHAPE,
        SUMMERSHAPE,
        AUTUMNSHAPE,
        WINTERSHAPE,
        GROWHP,
        SHRINKHP,
        XPSHAPE,
        XPLOSS,
        TRANSFORMATION,
        FIREATTUNED,
        AIRATTUNED,
        WATERATTUNED,
        EARTHATTUNED,
        ASTRALATTUNED,
        DEATHATTUNED,
        NATUREATTUNED,
        BLOODATTUNED,
        CLEANSHAPE,
        LANDSHAPE,
        WATERSHAPE,
        TWICEBORN,
        REANIMATOR,
        REANIMPRIEST,
        DOMSUMMON,
        DOMSUMMON2,
        DOMSUMMON20,
        RAREDOMSUMMON,
        TEMPLETRAINER,
        MAKEMONSTERS1,
        MAKEMONSTERS2,
        MAKEMONSTERS3,
        MAKEMONSTERS4,
        MAKEMONSTERS5,
        SUMMON1,
        SUMMON2,
        SUMMON3,
        SUMMON4,
        SUMMON5,
        BATTLESUM1,
        BATTLESUM2,
        BATTLESUM3,
        BATTLESUM4,
        BATTLESUM5,
        BATSTARTSUM1,
        BATSTARTSUM2,
        BATSTARTSUM3,
        BATSTARTSUM4,
        BATSTARTSUM5,
        BATSTARTSUM1D3,
        BATSTARTSUM1D6,
        BATSTARTSUM2D6,
        BATSTARTSUM3D6,
        BATSTARTSUM4D6,
        BATSTARTSUM5D6,
        BATSTARTSUM6D6,
        BATSTARTSUM7D6,
        BATSTARTSUM8D6,
        BATSTARTSUM9D6,
        MONTAG,
        MONTAGWEIGHT,
        IVYLORD,
        DRAGONLORD,
        LAMIALORD,
        CORPSELORD,
        ONISUMMON,
        SLAVER,
        SLAVERBONUS,
        NAMETYPE,
        NOLEADER,
        POORLEADER,
        OKLEADER,
        GOODLEADER,
        EXPERTLEADER,
        SUPERIORLEADER,
        COMMAND,
        NOMAGICLEADER,
        POORMAGICLEADER,
        OKMAGICLEADER,
        GOODMAGICLEADER,
        EXPERTMAGICLEADER,
        SUPERIORMAGICLEADER,
        MAGICCOMMAND,
        NOUNDEADLEADER,
        POORUNDEADLEADER,
        OKUNDEADLEADER,
        GOODUNDEADLEADER,
        EXPERTUNDEADLEADER,
        SUPERIORUNDEADLEADER,
        UNDCOMMAND,
        ALMOSTUNDEAD,
        ALMOSTLIVING,
        INSPIRATIONAL,
        BEASTMASTER,
        TASKMASTER,
        SLAVE,
        UNDISCIPLINED,
        FORMATIONFIGHTER,
        BODYGUARD,
        WARNING,
        STANDARD,
        LATEHERO,
        MAGICSKILL,
        CUSTOMMAGIC,
        MAGICBOOST,
        MASTERRIT,
        CHORUSMASTER,
        CHORUSSLAVE,
        FIRERANGE,
        AIRRANGE,
        WATERRANGE,
        EARTHRANGE,
        ASTRALRANGE,
        DEATHRANGE,
        NATURERANGE,
        BLOODRANGE,
        ELEMENTRANGE,
        SORCERYRANGE,
        ALLRANGE,
        FIXEDRESEARCH,
        RESEARCHBONUS,
        INSPIRINGRES,
        SLOTHRESEARCH,
        DRAINIMMUNE,
        MAGICIMMUNE,
        DIVINEINS,
        GEMPROD,
        TMPFIREGEMS,
        TMPAIRGEMS,
        TMPWATERGEMS,
        TMPEARTHGEMS,
        TMPASTRALGEMS,
        TMPDEATHGEMS,
        TMPNATUREGEMS,
        DOUSE,
        MAKEPEARLS,
        CARCASSCOLLECTOR,
        BONUSSPELLS,
        ONEBATTLESPELL,
        CROSSBREEDER,
        DEATHBANISH,
        KOKYTOSRET,
        INFERNORET,
        VOIDRET,
        ALLRET,
        RANDOMSPELL,
        TAINTED,
        FORGEBONUS,
        FIXFORGEBONUS,
        MASTERSMITH,
        COMMASTER,
        COMSLAVE,
        INDEPSPELLS,
        FASTCAST,
        SPELLSINGER,
        MAGICSTUDY,
        BRINGEROFFORTUNE,
        COMBATCASTER,
        UNSURR,
        SKIRMISHER,
        MINSIZELEADER,
        MCOST,
        LIMITEDREGEN,
        ENCHREBATE10,
        ENCHREBATE20,
        SAILSIZE,
        MINDVESSEL,
        SALTVUL,
        SHAPECHANCE,
        NOTSACRED,
        APPETITE,
        CLEARALLSPELLS,
        SELECTSPELL,
        NEWSPELL,
        COPYSPELL,
        DETAILS,
        SCHOOL,
        RESEARCHLEVEL,
        PATH,
        PATHLEVEL,
        FATIGUECOST,
        DAMAGE,
        DAMAGEMON,
        NEXTSPELL,
        NEXTINGEO,
        EFFECT,
        NREFF,
        PRECISION,
        FLIGHTSPR,
        STRIKESOUND,
        PROVRANGE,
        ONLYGEOSRC,
        NOGEOSRC,
        ONLYGEODST,
        NOGEODST,
        ONLYCOASTSRC,
        ONLYATSITE,
        ONLYFRIENDLYDST,
        ONLYOWNDST,
        NOWATERTRACE,
        NOLANDTRACE,
        WALKABLE,
        SPEC,
        RESTRICTED,
        FARSUMCOM,
        NOTFORNATION,
        CASTTIME,
        GODPATHSPELL,
        FRIENDLYENCH,
        HIDDENENCH,
        NOCASTMINDLESS,
        SPELLREQFLY,
        ONLYMNR,
        NOTMNR,
        POLYGETMAGIC,
        MAXBOUNCES,
        REQSPELLSINGER,
        REQTASKMASTER,
        REQSEDUCE,
        SETHOME,
        REQSUN,
        AINOCAST,
        AIBADLVL,
        AISPELLMOD,
        REQPLANT,
        REQNOPLANT,
        NEWITEM,
        SELECTITEM,
        CLEARALLITEMS,
        CONSTLEVEL,
        MAINPATH,
        MAINLEVEL,
        SECONDARYPATH,
        SECONDARYLEVEL,
        COPYITEM,
        SPR,
        PEN,
        SPELL,
        AUTOSPELL,
        AUTOSPELLREPEAT,
        LUCK,
        MORALE,
        QUICKNESS,
        BLESS,
        BARKSKIN,
        STONESKIN,
        IRONSKIN,
        BERS,
        EXTRALIFE,
        POLYIMMUNE,
        AUTOBLESS,
        MAPSPEED,
        WATERBREATHING,
        FLY,
        RUN,
        SNEAKUNIT,
        STEALTHBOOST,
        SWIFT,
        REQEYES,
        NOFIND,
        RESTRICTEDITEM,
        NATIONREBATE,
        NOFORGEBONUS,
        ISLANCE,
        MINSIZE,
        MAXSIZE,
        CURSED,
        NOMOUNTED,
        CURSE,
        NOCOLDBLOOD,
        DISEASE,
        NODEMON,
        CHESTWOUND,
        NOUNDEAD,
        NOINANIM,
        NOIMMOBILE,
        FEEBLEMIND,
        MUTE,
        ONLYMOUNTED,
        ONLYCOLDBLOOD,
        NHWOUND,
        ONLYDEMON,
        CRIPPLED,
        ONLYUNDEAD,
        LOSEEYE,
        ONLYINANIM,
        ONLYIMMOBILE,
        RECUPERATION,
        YEARAGING,
        NOAGING,
        NOAGINGLAND,
        ITEMCOST1,
        ITEMCOST2,
        ITEMDRAWSIZE,
        CHAMPPRIZE,
        TMPBLOODSLAVES,
        SELECTNAMETYPE,
        ADDNAME,
        NEWMERC,
        CLEARMERCS,
        LEVEL,
        BOSSNAME,
        COM,
        UNIT,
        NRUNITS,
        MINMEN,
        MINPAY,
        XP,
        RANDEQUIP,
        RECRATE,
        ITEM,
        ERAMASK,
        SELECTSITE,
        NEWSITE,
        GEMS,
        RES,
        RARITY,
        DECUNREST,
        SUPPLY,
        HOMEMON,
        HOMECOM,
        MON,
        NAT,
        NATMON,
        NATCOM,
        SUMMON,
        SUMMONLVL2,
        SUMMONLVL3,
        SUMMONLVL4,
        VOIDGATE,
        WALLCOM,
        WALLUNIT,
        WALLMULT,
        UWDEFCOM1,
        UWDEFCOM2,
        UWDEFUNIT1,
        UWDEFMULT1,
        UWDEFUNIT1B,
        UWDEFMULT1B,
        UWDEFUNIT2,
        UWDEFMULT2,
        UWDEFUNIT2B,
        UWDEFMULT2B,
        UWDEFUNIT1C,
        UWDEFMULT1C,
        UWDEFUNIT1D,
        UWDEFMULT1D,
        UWWALLUNIT,
        UWWALLMULT,
        UWWALLCOM,
        CONJCOST,
        ALTCOST,
        EVOCOST,
        CONSTCOST,
        ENCHCOST,
        THAUCOST,
        BLOODCOST,
        SCRY,
        SCRYRANGE,
        CLUSTER,
        HOLYFIRE,
        HOLYPOWER,
        ADVENTURERUIN,
        LAB,
        TEMPLE,
        FORT,
        CLAIM,
        DOMINION,
        GODDOMCHAOS,
        GODDOMLAZY,
        GODDOMCOLD,
        GODDOMDEATH,
        GODDOMMISFORTUNE,
        GODDOMDRAIN,
        BLESSHP,
        BLESSANIMAWE,
        BLESSMR,
        BLESSAWE,
        BLESSMOR,
        BLESSSTR,
        BLESSDARKVIS,
        BLESSATT,
        EVIL,
        BLESSDEF,
        BLESSPREC,
        BLESSFIRERES,
        BLESSCOLDRES,
        BLESSSHOCKRES,
        BLESSPOISRES,
        BLESSAIRSHLD,
        BLESSREINVIG,
        BLESSDTV,
        WILD,
        RECALLGOD,
        DOMWAR,
        MINEGOLD,
        INDEPFLAG,
        SELECTNATION,
        NEWNATION,
        CLEARNATION,
        EPITHET,
        ERA,
        SUMMARY,
        BRIEF,
        COLOR,
        SECONDARYCOLOR,
        FLAG,
        DISABLEOLDNATIONS,
        CLEARSITES,
        STARTSITE,
        ISLANDSITE,
        LIKESTERR,
        IDEALCOLD,
        DEFCHAOS,
        DEFSLOTH,
        DEFDEATH,
        DEFMISFORTUNE,
        DEFDRAIN,
        UWNATION,
        COASTNATION,
        RIVERSTART,
        CAVENATION,
        ISLANDNATION,
        HATESTERR,
        KILLCAPPOP,
        AIHOLDGOD,
        AIAWAKE,
        AIFIRENATION,
        AIAIRNATION,
        AIWATERNATION,
        AIEARTHNATION,
        AINATURENATION,
        AIASTRALNATION,
        AIDEATHNATION,
        AIBLOODNATION,
        BLOODNATION,
        AIGOODBLESS,
        AIMUSTHAVEMAG,
        AICHEAPHOLY,
        AIHOLYRANGED,
        AIHEAVYREC,
        AIMAGEREC,
        CLEARREC,
        STARTCOM,
        COASTCOM1,
        COASTCOM2,
        ADDFOREIGNUNIT,
        ADDFOREIGNCOM,
        FORESTREC,
        MOUNTAINREC,
        SWAMPREC,
        WASTEREC,
        CAVEREC,
        COASTREC,
        FORESTCOM,
        MOUNTAINCOM,
        SWAMPCOM,
        WASTECOM,
        CAVECOM,
        COASTCOM,
        STARTSCOUT,
        STARTUNITTYPE1,
        STARTUNITNBRS1,
        STARTUNITTYPE2,
        STARTUNITNBRS2,
        ADDRECUNIT,
        ADDRECCOM,
        UWREC,
        UWCOM,
        COASTUNIT1,
        COASTUNIT2,
        COASTUNIT3,
        LANDREC,
        LANDCOM,
        MERCCOST,
        HERO1,
        HERO2,
        HERO3,
        HERO4,
        HERO5,
        HERO6,
        HERO7,
        HERO8,
        HERO9,
        HERO10,
        MULTIHERO1,
        MULTIHERO2,
        MULTIHERO3,
        MULTIHERO4,
        MULTIHERO5,
        MULTIHERO6,
        MULTIHERO7,
        NOFOREIGNREC,
        DEFCOM1,
        DEFCOM2,
        DEFUNIT1,
        DEFUNIT1B,
        DEFUNIT1C,
        DEFUNIT1D,
        DEFUNIT2,
        DEFUNIT2B,
        DEFMULT1,
        DEFMULT1B,
        DEFMULT1C,
        DEFMULT1D,
        DEFMULT2,
        DEFMULT2B,
        BADINDPD,
        CLEARGODS,
        ADDGOD,
        NOUNDEADGODS,
        DELGOD,
        LIKESPOP,
        GODREBIRTH,
        CHEAPGOD20,
        UWBUILD,
        CHEAPGOD40,
        FIREBLESSBONUS,
        AIRBLESSBONUS,
        WATERBLESSBONUS,
        EARTHBLESSBONUS,
        ASTRALBLESSBONUS,
        DEATHBLESSBONUS,
        NATUREBLESSBONUS,
        BLOODBLESSBONUS,
        FORTERA,
        FORTCOST,
        LABCOST,
        TEMPLECOST,
        FORESTLABCOST,
        FORESTTEMPLECOST,
        TEMPLEPIC,
        TEMPLEGEMS,
        HOMEFORT,
        BUILDFORT,
        BUILDUWFORT,
        BUILDCOASTFORT,
        FORTUNREST,
        NODEATHSUPPLY,
        HALFDEATHINC,
        HALFDEATHPOP,
        DOMDEATHSENSE,
        NATIONINC,
        CASTLEPROD,
        TRADECOAST,
        GOLEMHP,
        SPREADCOLD,
        SPREADHEAT,
        SPREADCHAOS,
        SPREADLAZY,
        SPREADDEATH,
        NOPREACH,
        DYINGDOM,
        SACRIFICEDOM,
        DOMKILL,
        DOMUNREST,
        AUTOUNDEAD,
        GUARDSPIRIT,
        SYNCRETISM,
        DOMSAIL,
        PRIESTREANIM,
        UNDEADREANIM,
        HORSEREANIM,
        WIGHTREANIM,
        TOMBWYRMREANIM,
        MANIKINREANIM,
        SUPAYAREANIM,
        GREEKREANIM,
        GHOSTREANIM,
        SELECTPOPTYPE,
        CLEARDEF,
        POPPERGOLD,
        RESOURCEMULT,
        SUPPLYMULT,
        UNRESTHALFINC,
        UNRESTHALFRES,
        EVENTISRARE,
        TURMOILINCOME,
        TURMOILEVENTS,
        DEATHINCOME,
        DEATHSUPPLY,
        DEATHDEATH,
        SLOTHINCOME,
        SLOTHRESOURCES,
        COLDINCOME,
        COLDSUPPLY,
        TEMPSCALECAP,
        MISFORTUNE,
        LUCKEVENTS,
        RESEARCHSCALE,
        CAVELABCOST,
        CAVETEMPLECOST,
        SWAMPLABCOST,
        SWAMPTEMPLECOST,
        MOUNTLABCOST,
        MOUNTTEMPLECOST,
        WASTELABCOST,
        WASTETEMPLECOST,
        FUTURESITE,
        CLEARALLEVENTS,
        NEWEVENT,
        REQ_RARE,
        REQ_UNIQUE,
        REQ_STORY,
        REQ_INDEPOK,
        REQ_ERA,
        REQ_NOERA,
        REQ_TURN,
        REQ_MAXTURN,
        REQ_PREGAME,
        REQ_SEASON,
        REQ_NOSEASON,
        REQ_NATION,
        REQ_NONATION,
        REQ_FORNATION,
        REQ_NOTNATION,
        REQ_NOTFORNATION,
        REQ_NOTFORALLY,
        REQ_GEM,
        REQ_GOLD,
        REQ_CAPITAL,
        REQ_OWNCAPITAL,
        REQ_POPTYPE,
        REQ_POP0OK,
        REQ_MAXPOP,
        REQ_MINPOP,
        REQ_MINDEF,
        REQ_MAXDEF,
        REQ_MINUNREST,
        REQ_MAXUNREST,
        REQ_LAB,
        REQ_TEMPLE,
        REQ_FORT,
        REQ_LAND,
        REQ_COAST,
        REQ_MOUNTAIN,
        REQ_FOREST,
        REQ_FARM,
        REQ_SWAMP,
        REQ_WASTE,
        REQ_CAVE,
        REQ_FRESHWATER,
        REQ_FREESITES,
        REQ_NOSITENBR,
        REQ_FOUNDSITE,
        REQ_HIDDENSITE,
        REQ_SITE,
        REQ_NEARBYSITE,
        REQ_CLAIMEDTHRONE,
        REQ_UNCLAIMEDTHRONE,
        REQ_FULLOWNER,
        REQ_DOMOWNER,
        REQ_MYDOMINION,
        REQ_DOMINION,
        REQ_MAXDOMINION,
        REQ_DOMCHANCE,
        REQ_GODISMNR,
        REQ_CHAOS,
        REQ_LAZY,
        REQ_COLD,
        REQ_DEATH,
        REQ_UNLUCK,
        REQ_UNMAGIC,
        REQ_ORDER,
        REQ_PROD,
        REQ_HEAT,
        REQ_GROWTH,
        REQ_LUCK,
        REQ_MAGIC,
        REQ_COMMANDER,
        REQ_MONSTER,
        REQ_2MONSTERS,
        REQ_5MONSTERS,
        REQ_NOMONSTER,
        REQ_MNR,
        REQ_NOMNR,
        REQ_DEADMNR,
        REQ_MINTROOPS,
        REQ_MAXTROOPS,
        REQ_HUMANOIDRES,
        REQ_RESEARCHER,
        REQ_PREACH,
        REQ_PATHFIRE,
        REQ_PATHAIR,
        REQ_PATHWATER,
        REQ_PATHEARTH,
        REQ_PATHASTRAL,
        REQ_PATHDEATH,
        REQ_PATHNATURE,
        REQ_PATHBLOOD,
        REQ_PATHHOLY,
        REQ_NOPATHFIRE,
        REQ_NOPATHAIR,
        REQ_NOPATHWATER,
        REQ_NOPATHEARTH,
        REQ_NOPATHASTRAL,
        REQ_NOPATHDEATH,
        REQ_NOPATHNATURE,
        REQ_NOPATHBLOOD,
        REQ_NOPATHHOLY,
        REQ_NOPATHALL,
        REQ_TARGMNR,
        REQ_TARGNOMNR,
        REQ_TARGGOD,
        REQ_TARGPROPHET,
        REQ_TARGHUMANOID,
        REQ_TARGMALE,
        REQ_TARGPATH1,
        REQ_TARGPATH2,
        REQ_TARGPATH3,
        REQ_TARGPATH4,
        REQ_TARGNOPATH1,
        REQ_TARGNOPATH2,
        REQ_TARGNOPATH3,
        REQ_TARGNOPATH4,
        REQ_TARGAFF,
        REQ_TARGNOAFF,
        REQ_TARGORDER,
        REQ_TARGITEM,
        REQ_TARGNOITEM,
        REQ_TARGUNDEAD,
        REQ_TARGDEMON,
        REQ_TARGANIMAL,
        REQ_TARGINANIMATE,
        REQ_TARGMINDLESS,
        REQ_TARGIMMOBILE,
        REQ_TARGMAGICBEING,
        REQ_TARGOWNER,
        REQ_TARGFOREIGNOK,
        REQ_TARGMINSIZE,
        REQ_TARGMAXSIZE,
        REQ_CODE,
        REQ_ANYCODE,
        REQ_NOTANYCODE,
        REQ_NEARBYCODE,
        REQ_NEAROWNCODE,
        REQ_PERMONTH,
        REQ_NOENCH,
        REQ_ENCH,
        REQ_MYENCH,
        REQ_FRIENDLYENCH,
        REQ_HOSTILEENCH,
        REQ_ENCHDOM,
        REQ_ARENADONE,
        REQ_THRONESITE,
        NATION,
        NATIONENCH,
        MSG,
        NOTEXT,
        NOLOG,
        MAGICITEM,
        EXACTGOLD,
        INCSCALE2,
        INCSCALE3,
        ZZ1D3VIS,
        ZZ1D6VIS,
        ZZ2D4VIS,
        ZZ2D6VIS,
        ZZ3D6VIS,
        DECSCALE2,
        DECSCALE3,
        ZZ4D6VIS,
        GEMLOSS,
        LANDGOLD,
        LANDPROD,
        TAXBOOST,
        DEFENCE,
        KILL,
        KILLPOP,
        INCPOP,
        INCCORPSES,
        EMIGRATION,
        UNREST,
        INCDOM,
        REVEALSITE,
        ADDSITE,
        REMOVESITE,
        HIDDENSITE,
        VISITORS,
        NEWDOM,
        REVOLT,
        REVEALPROV,
        CLAIMTHRONE,
        ASSOWNER,
        STEALTHCOM,
        ZZ2COM,
        ZZ4COM,
        ZZ5COM,
        TEMPUNITS,
        ZZ1UNIT,
        ZZ1D3UNITS,
        ZZ2D3UNITS,
        ZZ3D3UNITS,
        ZZ4D3UNITS,
        ZZ1D6UNITS,
        ZZ2D6UNITS,
        ZZ3D6UNITS,
        ZZ4D6UNITS,
        ZZ5D6UNITS,
        ZZ6D6UNITS,
        ZZ7D6UNITS,
        ZZ8D6UNITS,
        ZZ9D6UNITS,
        ZZ10D6UNITS,
        ZZ11D6UNITS,
        ZZ12D6UNITS,
        ZZ13D6UNITS,
        ZZ14D6UNITS,
        ZZ15D6UNITS,
        ZZ16D6UNITS,
        STRIKEUNITS,
        KILLMON,
        KILL2D6MON,
        KILLCOM,
        RESEARCHAFF,
        GAINAFF,
        GAINMARK,
        BANISHED,
        ADDEQUIP,
        TRANSFORM,
        FIREBOOST,
        AIRBOOST,
        WATERBOOST,
        EARTHBOOST,
        ASTRALBOOST,
        DEATHBOOST,
        NATUREBOOST,
        BLOODBOOST,
        HOLYBOOST,
        PATHBOOST,
        WORLDINCSCALE,
        WORLDINCSCALE2,
        WORLDINCSCALE3,
        WORLDDECSCALE,
        WORLDDECSCALE2,
        WORLDDECSCALE3,
        WORLDUNREST,
        WORLDINCDOM,
        WORLDRITREBATE,
        WORLDDARKNESS,
        WORLDCURSE,
        WORLDDISEASE,
        WORLDMARK,
        WORLDHEAL,
        WORLDAGE,
        LINGER,
        FLAGLAND,
        DELAY,
        DELAY25,
        DELAY50,
        DELAYSKIP,
        ORDER,
        CODE,
        CODE2,
        RESETCODE,
        PURGECALENDAR,
        PURGEDELAYED,
        ID,
        CODEDELAY,
        CODEDELAY2,
        RESETCODEDELAY,
        RESETCODEDELAY2,
        ARENA,
        RESOLVEARENA1,
        RESOLVEARENA2,
        LOC,
        EXTRAMSG,
        REQ_MAXCORPSES,
        REQ_MINCORPSES,
        SETPOPTYPE,
        REQ_NATIVESOIL,
        REQ_TARGNOORDER,
        REQ_TARGMINMORALE,
        REQ_TARGMAXMORALE,
        REQ_TARGNOTOWNER,
        REQ_TARGNOTALLY,
        REQ_TARGALLY,
        REQ_GODISNOTMNR,
        REQ_WORLDITEM,
        REQ_NOWORLDITEM,
        SETXP,
        ACIDRES,
        DECAYRES,
        HEAVYITEM,
        NOFEMALE,
        ONLYFEMALE,
        DANCEWEAPON,
        DANCENRATT,
        DANCESPR,
        DANCENOF,
        DANCESIZE,
        BESTOWTOMOUNT,
        MOBILEARCHER,
        STATSTORM,
        STATBREAK,
        PLAGUEDOCTOR,
        CORRUPTOR,
        ICENATPROT,
        MAXDEADHP,
        XPGAIN,
        CLUMSY,
        TRUESIGHT,
        TWISTFATE,
        POWEROFDEATH,
        FEAROFFLOOD,
        MINDCOLLAR,
        FALSESUPPLY,
        ELEMENTGEMS,
        SORCERYGEMS,
        GLAMOURMANIP,
        GLAMOURRANGE,
        HOLYRANGE,
        TMPGLAMOURGEMS,
        FIREELEMENTALS,
        AIRELEMENTALS,
        EARTHELEMENTALS,
        WATERELEMENTALS,
        UNDISLEADER,
        THIRDSTR,
        SPIRITFORMIMMUNE,
        ILLUSIONSIMMUNE,
        FALSE,
        SPEEDMULT,
        NOTMOUNTED,
        NOTDISMOUNTED,
        HOLYIFHIT,
        KILLMAGICIFHIT,
        KILLDEMONIFHIT,
        HOLYSTUNIFHIT,
        PETRIFYIFHIT,
        FIREIFHIT,
        COLDIFHIT,
        SHOCKIFHIT,
        POISONIFDMG,
        AFTERCLOUD,
        AFTERCLOUDAREA,
        GODSITE,
        MOREORDER,
        MOREPROD,
        MOREHEAT,
        MOREGROWTH,
        MORELUCK,
        MOREMAGIC,
        ENCHREBATE75,
        ENCHREBATE100,
        HEATRECSCALE,
        COLDRECSCALE,
        GROWTHRECSCALE,
        DEATHRECSCALE,
        ORDERRECSCALE,
        CHAOSRECSCALE,
        NOWEAPON,
        HOLYCOST,
        SPIRITFORM,
        NOSPIRITFORM,
        DIVINEBEING,
        ASSENCLOC,
        AUTOCORPSEHEALER,
        EXTRALIVES,
        RECONST,
        FALSEREGEN,
        GLAMOUR,
        GLAMOURATTUNED,
        DREAD,
        NIGHTMAREAURA,
        AUTOBERSERK,
        DEATHPOISON,
        DEATHSHOCK,
        DEATHSLIME,
        DEATHGRAB,
        LABXPSHAPE,
        XPSHAPEMON,
        XPSHAPELOSS,
        LICH,
        BATTLESHAPE,
        WORLDSHAPE,
        ANIMATED,
        MOUNTMNR,
        UNMOUNTEDSPR1,
        UNMOUNTEDSPR2,
        NOFMOUNTS,
        NOFRIDERS,
        CORIDERMNR,
        XSPR1,
        XSPR2,
        REGAINMOUNT,
        NOREMOUNT,
        SKILLEDRIDER,
        MOUNTISCOM,
        NOBARDING,
        NOTHROWOFF,
        NOFALLDMG,
        BRAVEMOUNT,
        SMARTMOUNT,
        LOOK,
        DEFCOM,
        DEFUNIT,
        DEFMULT,
        POPGROWTH,
        VIEWALLPROV,
        VIEWALLBAT,
        AIGLAMOURNATION,
        GUARDCOM,
        GUARDUNIT,
        GUARDMULT,
        SEATRACE,
        DISBLESS,
        HIDEDOM,
        TEMPLEHOLYPOINTS,
        FORESTFORTREC,
        FORESTFORTCOM,
        MOUNTAINFORTREC,
        MOUNTAINFORTCOM,
        SWAMPFORTREC,
        SWAMPFORTCOM,
        WASTEFORTREC,
        WASTEFORTCOM,
        FARMREC,
        FARMFORTREC,
        FARMCOM,
        FARMFORTCOM,
        CAVEFORTREC,
        CAVEFORTCOM,
        DRIPREC,
        DRIPFORTREC,
        DRIPCOM,
        DRIPFORTCOM,
        COASTFORTREC,
        COASTFORTCOM,
        SEAREC,
        SEAFORTREC,
        SEACOM,
        SEAFORTCOM,
        DEEPREC,
        DEEPFORTREC,
        DEEPCOM,
        DEEPFORTCOM,
        KELPREC,
        KELPFORTREC,
        KELPCOM,
        KELPFORTCOM,
        ONLYSITEDST,
        DISPIMMUNE,
        NAPBREAKRIT,
        SUMHEALAFFS,
        LOCALGLOBAL,
        WORLDVISIBLE,
        GLOBALLOOK,
        PORTENT,
        CURE,
        REQNOSPELLSINGER,
        REQNOTASKMASTER,
        REQNOSEDUCE,
        AIASSMOD,
        REQ_TURNRARE,
        REQ_MONTH,
        REQ_AI,
        REQ_NEARBYCAPITAL,
        REQ_NOTPOPTYPE,
        REQ_VOIDOK,
        REQ_FORTID,
        REQ_PLANE,
        REQ_KELP,
        REQ_GORGE,
        REQ_DEEP,
        REQ_FORESTCAVE,
        REQ_DRIP,
        REQ_CRYSTAL,
        REQ_VOID,
        REQ_GODAWAKE,
        REQ_PRETISMNR,
        REQ_PRETAWAKE,
        REQ_PATHGLAMOUR,
        REQ_NOPATHGLAMOUR,
        REQ_TARGSIGHT,
        REQ_TARGMANYGEMS,
        REQ_TARGHORRORMARK,
        REQ_TARGINSANE,
        REQ_TARGSEDUCTIONS,
        REQ_TARGMINKILLS,
        REQ_TARGMAXKILLS,
        REQ_NOTCODE,
        REQ_ENCHTARGET,
        REQ_ENCHNEARBY,
        REQ_MINGLOBALS,
        REQ_MAXGLOBALS,
        REQ_VARPOS,
        REQ_VARNEG,
        REQ_VARZERO,
        REQ_VARONE,
        ASSOWNERENCH,
        ASSFOLLOWER1,
        ASSFOLLOWER2,
        ASSFOLLOWER3,
        ASSFOLLOWER1D3,
        HEALAFF,
        FORCETRANSFORM,
        REMOUNT,
        CLEARTARG,
        KILLTARG,
        ADDSEDUCTIONS,
        ADDKILLS,
        GLAMOURBOOST,
        DISPGLOBALS,
        CLEARVAR,
        INCVAR,
        DECVAR,
        INC10VAR,
        DEC10VAR,
        INVVAR,
        TOGGLEVAR,
        ADDASCENSION,
        MINASCENSION,
        REQ_NEARBYTHRONE,
        HEADER,
        FORCEGOLD,
        FORCEEXACTGOLD,
        FORCE1D3VIS,
        FORCE1D6VIS,
        FORCE2D4VIS,
        FORCE2D6VIS,
        FORCE3D6VIS,
        FORCE4D6VIS,
        GEMLOSSSMALL,
        GEMLOSSLARGE,
        ADDGEO,
        REMGEO,
        STARTRESEARCH,
        ARENAGOLD,
        ARENAGEMS,
        GEMLONGEVITY,

        //NON EDITABLE ONES
        CORRUPT, //corruption like grigori
        REINCARNATION, //recinarnation like yogi
        PETRIFY, //petrify like gorgon effect
        REALM2, //only monolith, two home realms
        REALM3, //not used
        TURMOILSUMMON,
        COLDSUMMON,
        PREANIMATOR,
        DREANIMATOR,
        HORROR,
        SIZEWHENSAILING,
        MUMMIFICATION,
        SENDLESSORHORRORMULT,
        CURSEATTACKER,
        DISBELIEVE,
        LANDREINVIGORATION,
        ASTRALFETTERS,
        FOREIGNMAGICBOOST,
        STUNIMMUNITY,
        ADVENTURERS,
        HORRORMARKED,
        CHANGETARGETGENDERFORSEDUCTIONANDSEDUCTIONIMMUNE,
        CORPSECONSTRUCT,
        ISASHAH,
        ISAYAZAD,
        ISADAEVA,
        CLOCKWORKLORD,
        STUPID,
        HEATHENSUMMON,
        UNSEEN,
        ILLUSIONARY,
        WOLF,
        ABOLETH,
        DEFILER,
        MOUNTEDBERSERK,
        LANDENC,
        PATHBOOSTUW,
        PATHBOOSTLAND,
        PERCENTPATHREDUCTION,
        LABPROMOTION,
        UNDYING,
        MORALEBONUS,
        UNCURABLEAFFLICTION,
        AUTOBLESSED,
        WINTERSUMMON1D3,
        STYGIANGUIDE,
        PROTPARTS,
        SELECTEVENT,
        // Dom6 additions for weapon
        DEFROLL,
        DMGINSPECTOR,
        MORROLL,
        // Dom6 additions for armor
        PROTINSPECTOR,
        // Dom6 additions for monster
        MOUNTEDINSPECTOR,
        GROWTHPOWER,
        ICEPROTINSPECTOR,
        PRAISE,
        MOUNTEDSPR1,
        MOUNTEDSPR2,
        ENCHANTEDBLOOD,
        REGAINMOUNTS,
        REGEN,
        COLRES,
        AMPHIBIOUS,
        // Dom6 additions for site
        COPYSITE,
        // Dom6 additions for nation
        BLESSBONUS,
        PLAINFORTREC,
        PLAINFORTCOM,
        FOREIGNFORTREC,
        FOREIGNFORTCOM,
        UWGUARDCOM,
        UWGUARDUNIT,
        UWGUARDMULT,
        FORTHEATSCALERES,
        FORTCOLDSCALERES,
        HOMEHEATSCALERES,
        PLAINREC,
        CAVEINC,
        CAVERES,
        CAVERECPT,
        // Dom6 additions for spell
        AOEINSPECTOR,
        FATIGUECOSTINSPECTOR,
        // Dom6 additions for item
        TAINT,
        // Dom6 additions for event
        REQ_TARGREALMNR,
        // Bless entity commands
        SELECTBLESS,
        PATH0,
        COST0,
        PATH1,
        COST1,
        CLEARSCALES,
        ORDERSCALE,
        PRODSCALE,
        HEATSCALE,
        GROWTHSCALE,
        LUCKSCALE,
        MAGICSCALE,
        CHAOSSCALE,
        SLOTHSCALE,
        COLDSCALE,
        DEATHSCALE,
        MISFORTSCALE,
        DRAINSCALE,
        CLEARFX,
        // Template entity commands (AI pretender design)
        NEWTEMPLATE,
        FORM,
        PRISON,
        DOMSTR,
        SCALE,
        RESEARCHGOAL,
        FAVRIT,
        // Read by the Dominions 6.37 parser (tools/dom6exe catalog), new in Dom5Parser
        ARENA1,
        ARENA2,
        BATTLESUM1D2,
        BATTLESUM1D3,
        BATTLESUMWARM,
        BEAUTY,
        BUGSHAPE,
        BUGSWARMSHAPE,
        BUGSWARMUWSHAPE,
        BUGUWSHAPE,
        CORPSEHEALER,
        FAYSUMMON,
        FOREIGNGUARDCOM,
        FOREIGNGUARDMULT,
        FOREIGNGUARDUNIT,
        FOREIGNWALLCOM,
        FOREIGNWALLMULT,
        FOREIGNWALLUNIT,
        FORESTSURV,
        GAINAFFMOUNT,
        GLAMOURBLESSBONUS,
        GRANDCOM,
        HEALAFFMOUNT,
        HOMECOLDSCALERES,
        MAGICONLY,
        MAKECRATER,
        MAYBEADDSITE,
        MAYBEHIDDENSITE,
        MOUNTSURV,
        MRHALF,
        NEWNBOR,
        NOTINDOORS,
        ONLYSLEEPERS,
        PLAINCOM,
        REMNBOR,
        REQ_CAVEFOREST,
        REQ_DEEPSEA,
        REQ_MINRESEARCH,
        REQ_MNRBS,
        REQ_MONSTERBS,
        REQ_NOREALMNR,
        REQ_PATH,
        REQ_PATHGEMS,
        REQ_PROVNBR,
        REQ_REALMNR,
        REQ_SCHOOL,
        REQ_TARGNOREALMNR,
        SABBATHMASTER,
        SABBATHSLAVE,
        SIZECOST,
        SPEC2,
        STARTUNITNBRS3,
        STARTUNITTYPE3,
        STATSIEGE,
        SWAMPSURV,
        TOLERATEUND,
        TWICEBORNCOST,
        VAR0UNITS,
        WASTESURV,
        ZZ3CASTBATTLESPELL,
    }

    public class CommandsMap
    {
        // Maps strings in mod files to their command representation
        private static Dictionary<string, Command> _commandMap = new Dictionary<string, Command>();
        // Reversed map of string -> command
        private static Dictionary<Command, string> _stringMap = new Dictionary<Command, string>();

        private static Dictionary<Command, string> _toolTipMap = new Dictionary<Command, string>();
        private static HashSet<Command> _nonEditable = new HashSet<Command>();
        private static HashSet<Command> _singularMap = new HashSet<Command>();

        static CommandsMap()
        {
            _commandMap.Add("#modname", Command.MODNAME);
            _commandMap.Add("#description", Command.DESCRIPTION);
            _commandMap.Add("#icon", Command.ICON);
            _commandMap.Add("#version", Command.VERSION);
            _commandMap.Add("#domversion", Command.DOMVERSION);
            _commandMap.Add("#end", Command.END);
            _commandMap.Add("#name", Command.NAME);
            _commandMap.Add("#clear", Command.CLEAR);
            _commandMap.Add("#copyweapon", Command.COPYWEAPON);
            _commandMap.Add("#dmg", Command.DMG);
            _commandMap.Add("#nratt", Command.NRATT);
            _commandMap.Add("#att", Command.ATT);
            _commandMap.Add("#def", Command.DEF);
            _commandMap.Add("#selectweapon", Command.SELECTWEAPON);
            _commandMap.Add("#newweapon", Command.NEWWEAPON);
            _commandMap.Add("#len", Command.LEN);
            _commandMap.Add("#twohanded", Command.TWOHANDED);
            _commandMap.Add("#sound", Command.SOUND);
            _commandMap.Add("#range", Command.RANGE);
            _commandMap.Add("#ammo", Command.AMMO);
            _commandMap.Add("#rcost", Command.RCOST);
            _commandMap.Add("#sample", Command.SAMPLE);
            _commandMap.Add("#natural", Command.NATURAL);
            _commandMap.Add("#dt_normal", Command.DT_NORMAL);
            _commandMap.Add("#dt_poison", Command.DT_POISON);
            _commandMap.Add("#dt_demon", Command.DT_DEMON);
            _commandMap.Add("#dt_small", Command.DT_SMALL);
            _commandMap.Add("#dt_magic", Command.DT_MAGIC);
            _commandMap.Add("#dt_large", Command.DT_LARGE);
            _commandMap.Add("#dt_constructonly", Command.DT_CONSTRUCTONLY);
            _commandMap.Add("#dt_raise", Command.DT_RAISE);
            _commandMap.Add("#dt_cap", Command.DT_CAP);
            _commandMap.Add("#dt_weakness", Command.DT_WEAKNESS);
            _commandMap.Add("#dt_holy", Command.DT_HOLY);
            _commandMap.Add("#dt_drain", Command.DT_DRAIN);
            _commandMap.Add("#dt_sizestun", Command.DT_SIZESTUN);
            _commandMap.Add("#dt_weapondrain", Command.DT_WEAPONDRAIN);
            _commandMap.Add("#dt_stun", Command.DT_STUN);
            _commandMap.Add("#dt_realstun", Command.DT_REALSTUN);
            _commandMap.Add("#dt_interrupt", Command.DT_INTERRUPT);
            _commandMap.Add("#dt_bouncekill", Command.DT_BOUNCEKILL);
            _commandMap.Add("#dt_paralyze", Command.DT_PARALYZE);
            _commandMap.Add("#dt_aff", Command.DT_AFF);
            _commandMap.Add("#poison", Command.POISON);
            _commandMap.Add("#acid", Command.ACID);
            _commandMap.Add("#slash", Command.SLASH);
            _commandMap.Add("#pierce", Command.PIERCE);
            _commandMap.Add("#blunt", Command.BLUNT);
            _commandMap.Add("#cold", Command.COLD);
            _commandMap.Add("#fire", Command.FIRE);
            _commandMap.Add("#shock", Command.SHOCK);
            _commandMap.Add("#magic", Command.MAGIC);
            _commandMap.Add("#armorpiercing", Command.ARMORPIERCING);
            _commandMap.Add("#armornegating", Command.ARMORNEGATING);
            _commandMap.Add("#nostr", Command.NOSTR);
            _commandMap.Add("#bowstr", Command.BOWSTR);
            _commandMap.Add("#halfstr", Command.HALFSTR);
            _commandMap.Add("#fullstr", Command.FULLSTR);
            _commandMap.Add("#mrnegates", Command.MRNEGATES);
            _commandMap.Add("#mrnegateseasily", Command.MRNEGATESEASILY);
            _commandMap.Add("#hardmrneg", Command.HARDMRNEG);
            _commandMap.Add("#sizeresist", Command.SIZERESIST);
            _commandMap.Add("#mind", Command.MIND);
            _commandMap.Add("#undeadimmune", Command.UNDEADIMMUNE);
            _commandMap.Add("#inanimateimmune", Command.INANIMATEIMMUNE);
            _commandMap.Add("#flyingimmune", Command.FLYINGIMMUNE);
            _commandMap.Add("#enemyimmune", Command.ENEMYIMMUNE);
            _commandMap.Add("#friendlyimmune", Command.FRIENDLYIMMUNE);
            _commandMap.Add("#undeadonly", Command.UNDEADONLY);
            _commandMap.Add("#sacredonly", Command.SACREDONLY);
            _commandMap.Add("#demononly", Command.DEMONONLY);
            _commandMap.Add("#demonundead", Command.DEMONUNDEAD);
            _commandMap.Add("#internal", Command.INTERNAL);
            _commandMap.Add("#aoe", Command.AOE);
            _commandMap.Add("#bonus", Command.BONUS);
            _commandMap.Add("#secondaryeffect", Command.SECONDARYEFFECT);
            _commandMap.Add("#secondaryeffectalways", Command.SECONDARYEFFECTALWAYS);
            _commandMap.Add("#ironweapon", Command.IRONWEAPON);
            _commandMap.Add("#woodenweapon", Command.WOODENWEAPON);
            _commandMap.Add("#iceweapon", Command.ICEWEAPON);
            _commandMap.Add("#charge", Command.CHARGE);
            _commandMap.Add("#flail", Command.FLAIL);
            _commandMap.Add("#norepel", Command.NOREPEL);
            _commandMap.Add("#unrepel", Command.UNREPEL);
            _commandMap.Add("#beam", Command.BEAM);
            _commandMap.Add("#range050", Command.RANGE050);
            _commandMap.Add("#range0", Command.RANGE0);
            _commandMap.Add("#melee50", Command.MELEE50);
            _commandMap.Add("#skip", Command.SKIP);
            _commandMap.Add("#skip2", Command.SKIP2);
            _commandMap.Add("#explspr", Command.EXPLSPR);
            _commandMap.Add("#flyspr", Command.FLYSPR);
            _commandMap.Add("#uwok", Command.UWOK);
            _commandMap.Add("#nouw", Command.NOUW);
            _commandMap.Add("#soulslaying", Command.SOULSLAYING);
            _commandMap.Add("#selectarmor", Command.SELECTARMOR);
            _commandMap.Add("#newarmor", Command.NEWARMOR);
            _commandMap.Add("#copyarmor", Command.COPYARMOR);
            _commandMap.Add("#type", Command.TYPE);
            _commandMap.Add("#prot", Command.PROT);
            _commandMap.Add("#enc", Command.ENC);
            _commandMap.Add("#magicarmor", Command.MAGICARMOR);
            _commandMap.Add("#ironarmor", Command.IRONARMOR);
            _commandMap.Add("#woodenarmor", Command.WOODENARMOR);
            _commandMap.Add("#selectmonster", Command.SELECTMONSTER);
            _commandMap.Add("#newmonster", Command.NEWMONSTER);
            _commandMap.Add("#fixedname", Command.FIXEDNAME);
            _commandMap.Add("#descr", Command.DESCR);
            _commandMap.Add("#spr1", Command.SPR1);
            _commandMap.Add("#spr2", Command.SPR2);
            _commandMap.Add("#speciallook", Command.SPECIALLOOK);
            _commandMap.Add("#drawsize", Command.DRAWSIZE);
            _commandMap.Add("#clearweapons", Command.CLEARWEAPONS);
            _commandMap.Add("#cleararmor", Command.CLEARARMOR);
            _commandMap.Add("#clearmagic", Command.CLEARMAGIC);
            _commandMap.Add("#clearspec", Command.CLEARSPEC);
            _commandMap.Add("#copystats", Command.COPYSTATS);
            _commandMap.Add("#copyspr", Command.COPYSPR);
            _commandMap.Add("#pathcost", Command.PATHCOST);
            _commandMap.Add("#startdom", Command.STARTDOM);
            _commandMap.Add("#homerealm", Command.HOMEREALM);
            _commandMap.Add("#gcost", Command.GCOST);
            _commandMap.Add("#triplegod", Command.TRIPLEGOD);
            _commandMap.Add("#triplegodmag", Command.TRIPLEGODMAG);
            _commandMap.Add("#unify", Command.UNIFY);
            _commandMap.Add("#triple3mon", Command.TRIPLE3MON);
            _commandMap.Add("#minprison", Command.MINPRISON);
            _commandMap.Add("#maxprison", Command.MAXPRISON);
            _commandMap.Add("#slowrec", Command.SLOWREC);
            _commandMap.Add("#noslowrec", Command.NOSLOWREC);
            _commandMap.Add("#reclimit", Command.RECLIMIT);
            _commandMap.Add("#enchrebate50", Command.ENCHREBATE50);
            _commandMap.Add("#enchrebate25p", Command.ENCHREBATE25P);
            _commandMap.Add("#enchrebate50p", Command.ENCHREBATE50P);
            _commandMap.Add("#reqlab", Command.REQLAB);
            _commandMap.Add("#noreqlab", Command.NOREQLAB);
            _commandMap.Add("#reqtemple", Command.REQTEMPLE);
            _commandMap.Add("#noreqtemple", Command.NOREQTEMPLE);
            _commandMap.Add("#heatrec", Command.HEATREC);
            _commandMap.Add("#coldrec", Command.COLDREC);
            _commandMap.Add("#chaosrec", Command.CHAOSREC);
            _commandMap.Add("#deathrec", Command.DEATHREC);
            _commandMap.Add("#aisinglerec", Command.AISINGLEREC);
            _commandMap.Add("#ainorec", Command.AINOREC);
            _commandMap.Add("#monpresentrec", Command.MONPRESENTREC);
            _commandMap.Add("#ownsmonrec", Command.OWNSMONREC);
            _commandMap.Add("#domrec", Command.DOMREC);
            _commandMap.Add("#singlebattle", Command.SINGLEBATTLE);
            _commandMap.Add("#deserter", Command.DESERTER);
            _commandMap.Add("#horrordeserter", Command.HORRORDESERTER);
            _commandMap.Add("#defector", Command.DEFECTOR);
            _commandMap.Add("#nowish", Command.NOWISH);
            _commandMap.Add("#rpcost", Command.RPCOST);
            _commandMap.Add("#ressize", Command.RESSIZE);
            _commandMap.Add("#hp", Command.HP);
            _commandMap.Add("#str", Command.STR);
            _commandMap.Add("#prec", Command.PREC);
            _commandMap.Add("#size", Command.SIZE);
            _commandMap.Add("#mr", Command.MR);
            _commandMap.Add("#mor", Command.MOR);
            _commandMap.Add("#mapmove", Command.MAPMOVE);
            _commandMap.Add("#ap", Command.AP);
            _commandMap.Add("#eyes", Command.EYES);
            _commandMap.Add("#voidsanity", Command.VOIDSANITY);
            _commandMap.Add("#weapon", Command.WEAPON);
            _commandMap.Add("#armor", Command.ARMOR);
            _commandMap.Add("#humanoid", Command.HUMANOID);
            _commandMap.Add("#mountedhumanoid", Command.MOUNTEDHUMANOID);
            _commandMap.Add("#quadruped", Command.QUADRUPED);
            _commandMap.Add("#lizard", Command.LIZARD);
            _commandMap.Add("#naga", Command.NAGA);
            _commandMap.Add("#snake", Command.SNAKE);
            _commandMap.Add("#bird", Command.BIRD);
            _commandMap.Add("#djinn", Command.DJINN);
            _commandMap.Add("#troglodyte", Command.TROGLODYTE);
            _commandMap.Add("#miscshape", Command.MISCSHAPE);
            _commandMap.Add("#startitem", Command.STARTITEM);
            _commandMap.Add("#userestricteditem", Command.USERESTRICTEDITEM);
            _commandMap.Add("#noitem", Command.NOITEM);
            _commandMap.Add("#itemslots", Command.ITEMSLOTS);
            _commandMap.Add("#female", Command.FEMALE);
            _commandMap.Add("#coldblood", Command.COLDBLOOD);
            _commandMap.Add("#drake", Command.DRAKE);
            _commandMap.Add("#plant", Command.PLANT);
            _commandMap.Add("#lesserhorror", Command.LESSERHORROR);
            _commandMap.Add("#greaterhorror", Command.GREATERHORROR);
            _commandMap.Add("#doomhorror", Command.DOOMHORROR);
            _commandMap.Add("#mounted", Command.MOUNTED);
            _commandMap.Add("#holy", Command.HOLY);
            _commandMap.Add("#animal", Command.ANIMAL);
            _commandMap.Add("#unique", Command.UNIQUE);
            _commandMap.Add("#undead", Command.UNDEAD);
            _commandMap.Add("#bug", Command.BUG);
            _commandMap.Add("#demon", Command.DEMON);
            _commandMap.Add("#magicbeing", Command.MAGICBEING);
            _commandMap.Add("#autocompete", Command.AUTOCOMPETE);
            _commandMap.Add("#blind", Command.BLIND);
            _commandMap.Add("#uwbug", Command.UWBUG);
            _commandMap.Add("#stonebeing", Command.STONEBEING);
            _commandMap.Add("#inanimate", Command.INANIMATE);
            _commandMap.Add("#mindless", Command.MINDLESS);
            _commandMap.Add("#dungeon", Command.DUNGEON);
            _commandMap.Add("#lanceok", Command.LANCEOK);
            _commandMap.Add("#immobile", Command.IMMOBILE);
            _commandMap.Add("#aquatic", Command.AQUATIC);
            _commandMap.Add("#amphibian", Command.AMPHIBIAN);
            _commandMap.Add("#pooramphibian", Command.POORAMPHIBIAN);
            _commandMap.Add("#float", Command.FLOAT);
            _commandMap.Add("#flying", Command.FLYING);
            _commandMap.Add("#swimming", Command.SWIMMING);
            _commandMap.Add("#snow", Command.SNOW);
            _commandMap.Add("#stormimmune", Command.STORMIMMUNE);
            _commandMap.Add("#teleport", Command.TELEPORT);
            _commandMap.Add("#mapteleport", Command.MAPTELEPORT);
            _commandMap.Add("#blink", Command.BLINK);
            _commandMap.Add("#unteleportable", Command.UNTELEPORTABLE);
            _commandMap.Add("#noriverpass", Command.NORIVERPASS);
            _commandMap.Add("#forestsurvival", Command.FORESTSURVIVAL);
            _commandMap.Add("#mountainsurvival", Command.MOUNTAINSURVIVAL);
            _commandMap.Add("#swampsurvival", Command.SWAMPSURVIVAL);
            _commandMap.Add("#wastesurvival", Command.WASTESURVIVAL);
            _commandMap.Add("#sailing", Command.SAILING);
            _commandMap.Add("#giftofwater", Command.GIFTOFWATER);
            _commandMap.Add("#indepmove", Command.INDEPMOVE);
            _commandMap.Add("#indepstay", Command.INDEPSTAY);
            _commandMap.Add("#norange", Command.NORANGE);
            _commandMap.Add("#nomovepen", Command.NOMOVEPEN);
            _commandMap.Add("#farsail", Command.FARSAIL);
            _commandMap.Add("#illusion", Command.ILLUSION);
            _commandMap.Add("#seduce", Command.SEDUCE);
            _commandMap.Add("#succubus", Command.SUCCUBUS);
            _commandMap.Add("#stealthy", Command.STEALTHY);
            _commandMap.Add("#spy", Command.SPY);
            _commandMap.Add("#assassin", Command.ASSASSIN);
            _commandMap.Add("#patience", Command.PATIENCE);
            _commandMap.Add("#scalewalls", Command.SCALEWALLS);
            _commandMap.Add("#beckon", Command.BECKON);
            _commandMap.Add("#falsearmy", Command.FALSEARMY);
            _commandMap.Add("#foolscouts", Command.FOOLSCOUTS);
            _commandMap.Add("#startage", Command.STARTAGE);
            _commandMap.Add("#maxage", Command.MAXAGE);
            _commandMap.Add("#older", Command.OLDER);
            _commandMap.Add("#addrandomage", Command.ADDRANDOMAGE);
            _commandMap.Add("#uwdamage", Command.UWDAMAGE);
            _commandMap.Add("#heal", Command.HEAL);
            _commandMap.Add("#noheal", Command.NOHEAL);
            _commandMap.Add("#landdamage", Command.LANDDAMAGE);
            _commandMap.Add("#healer", Command.HEALER);
            _commandMap.Add("#homesick", Command.HOMESICK);
            _commandMap.Add("#autohealer", Command.AUTOHEALER);
            _commandMap.Add("#autodishealer", Command.AUTODISHEALER);
            _commandMap.Add("#autodisgrinder", Command.AUTODISGRINDER);
            _commandMap.Add("#diseaseres", Command.DISEASERES);
            _commandMap.Add("#woundfend", Command.WOUNDFEND);
            _commandMap.Add("#hpoverflow", Command.HPOVERFLOW);
            _commandMap.Add("#hpoverslow", Command.HPOVERSLOW);
            _commandMap.Add("#corpseeater", Command.CORPSEEATER);
            _commandMap.Add("#deadhp", Command.DEADHP);
            _commandMap.Add("#startaff", Command.STARTAFF);
            _commandMap.Add("#startmajoraff", Command.STARTMAJORAFF);
            _commandMap.Add("#startingaff", Command.STARTINGAFF);
            _commandMap.Add("#insane", Command.INSANE);
            _commandMap.Add("#startheroab", Command.STARTHEROAB);
            _commandMap.Add("#bluntres", Command.BLUNTRES);
            _commandMap.Add("#ethereal", Command.ETHEREAL);
            _commandMap.Add("#coldres", Command.COLDRES);
            _commandMap.Add("#fireres", Command.FIRERES);
            _commandMap.Add("#poisonres", Command.POISONRES);
            _commandMap.Add("#shockres", Command.SHOCKRES);
            _commandMap.Add("#iceprot", Command.ICEPROT);
            _commandMap.Add("#invulnerable", Command.INVULNERABLE);
            _commandMap.Add("#regeneration", Command.REGENERATION);
            _commandMap.Add("#doheal", Command.DOHEAL);
            _commandMap.Add("#undregen", Command.UNDREGEN);
            _commandMap.Add("#uwregen", Command.UWREGEN);
            _commandMap.Add("#pierceres", Command.PIERCERES);
            _commandMap.Add("#slashres", Command.SLASHRES);
            _commandMap.Add("#reinvigoration", Command.REINVIGORATION);
            _commandMap.Add("#airshield", Command.AIRSHIELD);
            _commandMap.Add("#ironvul", Command.IRONVUL);
            _commandMap.Add("#immortal", Command.IMMORTAL);
            _commandMap.Add("#domimmortal", Command.DOMIMMORTAL);
            _commandMap.Add("#forcess", Command.FORCESS);
            _commandMap.Add("#spikes", Command.SPIKES);
            _commandMap.Add("#sleepres", Command.SLEEPRES);
            _commandMap.Add("#reformtime", Command.REFORMTIME);
            _commandMap.Add("#springimmortal", Command.SPRINGIMMORTAL);
            _commandMap.Add("#reform", Command.REFORM);
            _commandMap.Add("#bugreform", Command.BUGREFORM);
            _commandMap.Add("#heat", Command.HEAT);
            _commandMap.Add("#uwheat", Command.UWHEAT);
            _commandMap.Add("#poisonarmor", Command.POISONARMOR);
            _commandMap.Add("#poisonskin", Command.POISONSKIN);
            _commandMap.Add("#poisoncloud", Command.POISONCLOUD);
            _commandMap.Add("#diseasecloud", Command.DISEASECLOUD);
            _commandMap.Add("#animalawe", Command.ANIMALAWE);
            _commandMap.Add("#awe", Command.AWE);
            _commandMap.Add("#sunawe", Command.SUNAWE);
            _commandMap.Add("#haltheretic", Command.HALTHERETIC);
            _commandMap.Add("#fear", Command.FEAR);
            _commandMap.Add("#fireshield", Command.FIRESHIELD);
            _commandMap.Add("#uwfireshield", Command.UWFIRESHIELD);
            _commandMap.Add("#banefireshield", Command.BANEFIRESHIELD);
            _commandMap.Add("#acidshield", Command.ACIDSHIELD);
            _commandMap.Add("#curseluckshield", Command.CURSELUCKSHIELD);
            _commandMap.Add("#damagerev", Command.DAMAGEREV);
            _commandMap.Add("#bloodvengeance", Command.BLOODVENGEANCE);
            _commandMap.Add("#slimer", Command.SLIMER);
            _commandMap.Add("#entangle", Command.ENTANGLE);
            _commandMap.Add("#eyeloss", Command.EYELOSS);
            _commandMap.Add("#horrormark", Command.HORRORMARK);
            _commandMap.Add("#mindslime", Command.MINDSLIME);
            _commandMap.Add("#overcharged", Command.OVERCHARGED);
            _commandMap.Add("#sleepaura", Command.SLEEPAURA);
            _commandMap.Add("#springpower", Command.SPRINGPOWER);
            _commandMap.Add("#summerpower", Command.SUMMERPOWER);
            _commandMap.Add("#fallpower", Command.FALLPOWER);
            _commandMap.Add("#winterpower", Command.WINTERPOWER);
            _commandMap.Add("#yearturn", Command.YEARTURN);
            _commandMap.Add("#chaospower", Command.CHAOSPOWER);
            _commandMap.Add("#coldpower", Command.COLDPOWER);
            _commandMap.Add("#firepower", Command.FIREPOWER);
            _commandMap.Add("#deathpower", Command.DEATHPOWER);
            _commandMap.Add("#darkpower", Command.DARKPOWER);
            _commandMap.Add("#stormpower", Command.STORMPOWER);
            _commandMap.Add("#magicpower", Command.MAGICPOWER);
            _commandMap.Add("#slothpower", Command.SLOTHPOWER);
            _commandMap.Add("#dompower", Command.DOMPOWER);
            _commandMap.Add("#ambidextrous", Command.AMBIDEXTROUS);
            _commandMap.Add("#berserk", Command.BERSERK);
            _commandMap.Add("#blessbers", Command.BLESSBERS);
            _commandMap.Add("#blessfly", Command.BLESSFLY);
            _commandMap.Add("#darkvision", Command.DARKVISION);
            _commandMap.Add("#spiritsight", Command.SPIRITSIGHT);
            _commandMap.Add("#invisible", Command.INVISIBLE);
            _commandMap.Add("#deathcurse", Command.DEATHCURSE);
            _commandMap.Add("#deathdisease", Command.DEATHDISEASE);
            _commandMap.Add("#deathparalyze", Command.DEATHPARALYZE);
            _commandMap.Add("#deathfire", Command.DEATHFIRE);
            _commandMap.Add("#guardspiritbonus", Command.GUARDSPIRITBONUS);
            _commandMap.Add("#trampswallow", Command.TRAMPSWALLOW);
            _commandMap.Add("#raiseonkill", Command.RAISEONKILL);
            _commandMap.Add("#trample", Command.TRAMPLE);
            _commandMap.Add("#digest", Command.DIGEST);
            _commandMap.Add("#aciddigest", Command.ACIDDIGEST);
            _commandMap.Add("#incorporate", Command.INCORPORATE);
            _commandMap.Add("#raiseshape", Command.RAISESHAPE);
            _commandMap.Add("#beartattoo", Command.BEARTATTOO);
            _commandMap.Add("#horsetattoo", Command.HORSETATTOO);
            _commandMap.Add("#wolftattoo", Command.WOLFTATTOO);
            _commandMap.Add("#boartattoo", Command.BOARTATTOO);
            _commandMap.Add("#snaketattoo", Command.SNAKETATTOO);
            _commandMap.Add("#castledef", Command.CASTLEDEF);
            _commandMap.Add("#siegebonus", Command.SIEGEBONUS);
            _commandMap.Add("#patrolbonus", Command.PATROLBONUS);
            _commandMap.Add("#pillagebonus", Command.PILLAGEBONUS);
            _commandMap.Add("#inquisitor", Command.INQUISITOR);
            _commandMap.Add("#supplybonus", Command.SUPPLYBONUS);
            _commandMap.Add("#heretic", Command.HERETIC);
            _commandMap.Add("#resources", Command.RESOURCES);
            _commandMap.Add("#iceforging", Command.ICEFORGING);
            _commandMap.Add("#neednoteat", Command.NEEDNOTEAT);
            _commandMap.Add("#elegist", Command.ELEGIST);
            _commandMap.Add("#spreaddom", Command.SPREADDOM);
            _commandMap.Add("#nobadevents", Command.NOBADEVENTS);
            _commandMap.Add("#shatteredsoul", Command.SHATTEREDSOUL);
            _commandMap.Add("#incunrest", Command.INCUNREST);
            _commandMap.Add("#incprovdef", Command.INCPROVDEF);
            _commandMap.Add("#taxcollector", Command.TAXCOLLECTOR);
            _commandMap.Add("#gold", Command.GOLD);
            _commandMap.Add("#addupkeep", Command.ADDUPKEEP);
            _commandMap.Add("#leper", Command.LEPER);
            _commandMap.Add("#popkill", Command.POPKILL);
            _commandMap.Add("#insanify", Command.INSANIFY);
            _commandMap.Add("#nohof", Command.NOHOF);
            _commandMap.Add("#alchemy", Command.ALCHEMY);
            _commandMap.Add("#mason", Command.MASON);
            _commandMap.Add("#incscale", Command.INCSCALE);
            _commandMap.Add("#decscale", Command.DECSCALE);
            _commandMap.Add("#fortkill", Command.FORTKILL);
            _commandMap.Add("#thronekill", Command.THRONEKILL);
            _commandMap.Add("#farthronekill", Command.FARTHRONEKILL);
            _commandMap.Add("#localsun", Command.LOCALSUN);
            _commandMap.Add("#adeptsacr", Command.ADEPTSACR);
            _commandMap.Add("#shapechange", Command.SHAPECHANGE);
            _commandMap.Add("#prophetshape", Command.PROPHETSHAPE);
            _commandMap.Add("#firstshape", Command.FIRSTSHAPE);
            _commandMap.Add("#secondshape", Command.SECONDSHAPE);
            _commandMap.Add("#secondtmpshape", Command.SECONDTMPSHAPE);
            _commandMap.Add("#forestshape", Command.FORESTSHAPE);
            _commandMap.Add("#plainshape", Command.PLAINSHAPE);
            _commandMap.Add("#foreignshape", Command.FOREIGNSHAPE);
            _commandMap.Add("#homeshape", Command.HOMESHAPE);
            _commandMap.Add("#domshape", Command.DOMSHAPE);
            _commandMap.Add("#notdomshape", Command.NOTDOMSHAPE);
            _commandMap.Add("#springshape", Command.SPRINGSHAPE);
            _commandMap.Add("#summershape", Command.SUMMERSHAPE);
            _commandMap.Add("#autumnshape", Command.AUTUMNSHAPE);
            _commandMap.Add("#wintershape", Command.WINTERSHAPE);
            _commandMap.Add("#growhp", Command.GROWHP);
            _commandMap.Add("#shrinkhp", Command.SHRINKHP);
            _commandMap.Add("#xpshape", Command.XPSHAPE);
            _commandMap.Add("#xploss", Command.XPLOSS);
            _commandMap.Add("#transformation", Command.TRANSFORMATION);
            _commandMap.Add("#fireattuned", Command.FIREATTUNED);
            _commandMap.Add("#airattuned", Command.AIRATTUNED);
            _commandMap.Add("#waterattuned", Command.WATERATTUNED);
            _commandMap.Add("#earthattuned", Command.EARTHATTUNED);
            _commandMap.Add("#astralattuned", Command.ASTRALATTUNED);
            _commandMap.Add("#deathattuned", Command.DEATHATTUNED);
            _commandMap.Add("#natureattuned", Command.NATUREATTUNED);
            _commandMap.Add("#bloodattuned", Command.BLOODATTUNED);
            _commandMap.Add("#cleanshape", Command.CLEANSHAPE);
            _commandMap.Add("#landshape", Command.LANDSHAPE);
            _commandMap.Add("#watershape", Command.WATERSHAPE);
            _commandMap.Add("#twiceborn", Command.TWICEBORN);
            _commandMap.Add("#reanimator", Command.REANIMATOR);
            _commandMap.Add("#reanimpriest", Command.REANIMPRIEST);
            _commandMap.Add("#domsummon", Command.DOMSUMMON);
            _commandMap.Add("#domsummon2", Command.DOMSUMMON2);
            _commandMap.Add("#domsummon20", Command.DOMSUMMON20);
            _commandMap.Add("#raredomsummon", Command.RAREDOMSUMMON);
            _commandMap.Add("#templetrainer", Command.TEMPLETRAINER);
            _commandMap.Add("#makemonsters1", Command.MAKEMONSTERS1);
            _commandMap.Add("#makemonsters2", Command.MAKEMONSTERS2);
            _commandMap.Add("#makemonsters3", Command.MAKEMONSTERS3);
            _commandMap.Add("#makemonsters4", Command.MAKEMONSTERS4);
            _commandMap.Add("#makemonsters5", Command.MAKEMONSTERS5);
            _commandMap.Add("#summon1", Command.SUMMON1);
            _commandMap.Add("#summon2", Command.SUMMON2);
            _commandMap.Add("#summon3", Command.SUMMON3);
            _commandMap.Add("#summon4", Command.SUMMON4);
            _commandMap.Add("#summon5", Command.SUMMON5);
            _commandMap.Add("#battlesum1", Command.BATTLESUM1);
            _commandMap.Add("#battlesum2", Command.BATTLESUM2);
            _commandMap.Add("#battlesum3", Command.BATTLESUM3);
            _commandMap.Add("#battlesum4", Command.BATTLESUM4);
            _commandMap.Add("#battlesum5", Command.BATTLESUM5);
            _commandMap.Add("#batstartsum1", Command.BATSTARTSUM1);
            _commandMap.Add("#batstartsum2", Command.BATSTARTSUM2);
            _commandMap.Add("#batstartsum3", Command.BATSTARTSUM3);
            _commandMap.Add("#batstartsum4", Command.BATSTARTSUM4);
            _commandMap.Add("#batstartsum5", Command.BATSTARTSUM5);
            _commandMap.Add("#batstartsum1d3", Command.BATSTARTSUM1D3);
            _commandMap.Add("#batstartsum1d6", Command.BATSTARTSUM1D6);
            _commandMap.Add("#batstartsum2d6", Command.BATSTARTSUM2D6);
            _commandMap.Add("#batstartsum3d6", Command.BATSTARTSUM3D6);
            _commandMap.Add("#batstartsum4d6", Command.BATSTARTSUM4D6);
            _commandMap.Add("#batstartsum5d6", Command.BATSTARTSUM5D6);
            _commandMap.Add("#batstartsum6d6", Command.BATSTARTSUM6D6);
            _commandMap.Add("#batstartsum7d6", Command.BATSTARTSUM7D6);
            _commandMap.Add("#batstartsum8d6", Command.BATSTARTSUM8D6);
            _commandMap.Add("#batstartsum9d6", Command.BATSTARTSUM9D6);
            _commandMap.Add("#montag", Command.MONTAG);
            _commandMap.Add("#montagweight", Command.MONTAGWEIGHT);
            _commandMap.Add("#ivylord", Command.IVYLORD);
            _commandMap.Add("#dragonlord", Command.DRAGONLORD);
            _commandMap.Add("#lamialord", Command.LAMIALORD);
            _commandMap.Add("#corpselord", Command.CORPSELORD);
            _commandMap.Add("#onisummon", Command.ONISUMMON);
            _commandMap.Add("#slaver", Command.SLAVER);
            _commandMap.Add("#slaverbonus", Command.SLAVERBONUS);
            _commandMap.Add("#nametype", Command.NAMETYPE);
            _commandMap.Add("#noleader", Command.NOLEADER);
            _commandMap.Add("#poorleader", Command.POORLEADER);
            _commandMap.Add("#okleader", Command.OKLEADER);
            _commandMap.Add("#goodleader", Command.GOODLEADER);
            _commandMap.Add("#expertleader", Command.EXPERTLEADER);
            _commandMap.Add("#superiorleader", Command.SUPERIORLEADER);
            _commandMap.Add("#command", Command.COMMAND);
            _commandMap.Add("#nomagicleader", Command.NOMAGICLEADER);
            _commandMap.Add("#poormagicleader", Command.POORMAGICLEADER);
            _commandMap.Add("#okmagicleader", Command.OKMAGICLEADER);
            _commandMap.Add("#goodmagicleader", Command.GOODMAGICLEADER);
            _commandMap.Add("#expertmagicleader", Command.EXPERTMAGICLEADER);
            _commandMap.Add("#superiormagicleader", Command.SUPERIORMAGICLEADER);
            _commandMap.Add("#magiccommand", Command.MAGICCOMMAND);
            _commandMap.Add("#noundeadleader", Command.NOUNDEADLEADER);
            _commandMap.Add("#poorundeadleader", Command.POORUNDEADLEADER);
            _commandMap.Add("#okundeadleader", Command.OKUNDEADLEADER);
            _commandMap.Add("#goodundeadleader", Command.GOODUNDEADLEADER);
            _commandMap.Add("#expertundeadleader", Command.EXPERTUNDEADLEADER);
            _commandMap.Add("#superiorundeadleader", Command.SUPERIORUNDEADLEADER);
            _commandMap.Add("#undcommand", Command.UNDCOMMAND);
            _commandMap.Add("#almostundead", Command.ALMOSTUNDEAD);
            _commandMap.Add("#almostliving", Command.ALMOSTLIVING);
            _commandMap.Add("#inspirational", Command.INSPIRATIONAL);
            _commandMap.Add("#beastmaster", Command.BEASTMASTER);
            _commandMap.Add("#taskmaster", Command.TASKMASTER);
            _commandMap.Add("#slave", Command.SLAVE);
            _commandMap.Add("#undisciplined", Command.UNDISCIPLINED);
            _commandMap.Add("#formationfighter", Command.FORMATIONFIGHTER);
            _commandMap.Add("#bodyguard", Command.BODYGUARD);
            _commandMap.Add("#warning", Command.WARNING);
            _commandMap.Add("#standard", Command.STANDARD);
            _commandMap.Add("#latehero", Command.LATEHERO);
            _commandMap.Add("#magicskill", Command.MAGICSKILL);
            _commandMap.Add("#custommagic", Command.CUSTOMMAGIC);
            _commandMap.Add("#magicboost", Command.MAGICBOOST);
            _commandMap.Add("#masterrit", Command.MASTERRIT);
            _commandMap.Add("#chorusmaster", Command.CHORUSMASTER);
            _commandMap.Add("#chorusslave", Command.CHORUSSLAVE);
            _commandMap.Add("#firerange", Command.FIRERANGE);
            _commandMap.Add("#airrange", Command.AIRRANGE);
            _commandMap.Add("#waterrange", Command.WATERRANGE);
            _commandMap.Add("#earthrange", Command.EARTHRANGE);
            _commandMap.Add("#astralrange", Command.ASTRALRANGE);
            _commandMap.Add("#deathrange", Command.DEATHRANGE);
            _commandMap.Add("#naturerange", Command.NATURERANGE);
            _commandMap.Add("#bloodrange", Command.BLOODRANGE);
            _commandMap.Add("#elementrange", Command.ELEMENTRANGE);
            _commandMap.Add("#sorceryrange", Command.SORCERYRANGE);
            _commandMap.Add("#allrange", Command.ALLRANGE);
            _commandMap.Add("#fixedresearch", Command.FIXEDRESEARCH);
            _commandMap.Add("#researchbonus", Command.RESEARCHBONUS);
            _commandMap.Add("#inspiringres", Command.INSPIRINGRES);
            _commandMap.Add("#slothresearch", Command.SLOTHRESEARCH);
            _commandMap.Add("#drainimmune", Command.DRAINIMMUNE);
            _commandMap.Add("#magicimmune", Command.MAGICIMMUNE);
            _commandMap.Add("#divineins", Command.DIVINEINS);
            _commandMap.Add("#gemprod", Command.GEMPROD);
            _commandMap.Add("#tmpfiregems", Command.TMPFIREGEMS);
            _commandMap.Add("#tmpairgems", Command.TMPAIRGEMS);
            _commandMap.Add("#tmpwatergems", Command.TMPWATERGEMS);
            _commandMap.Add("#tmpearthgems", Command.TMPEARTHGEMS);
            _commandMap.Add("#tmpastralgems", Command.TMPASTRALGEMS);
            _commandMap.Add("#tmpdeathgems", Command.TMPDEATHGEMS);
            _commandMap.Add("#tmpnaturegems", Command.TMPNATUREGEMS);
            _commandMap.Add("#douse", Command.DOUSE);
            _commandMap.Add("#makepearls", Command.MAKEPEARLS);
            _commandMap.Add("#carcasscollector", Command.CARCASSCOLLECTOR);
            _commandMap.Add("#bonusspells", Command.BONUSSPELLS);
            _commandMap.Add("#onebattlespell", Command.ONEBATTLESPELL);
            _commandMap.Add("#3castbattlespell", Command.ZZ3CASTBATTLESPELL);
            _commandMap.Add("#crossbreeder", Command.CROSSBREEDER);
            _commandMap.Add("#deathbanish", Command.DEATHBANISH);
            _commandMap.Add("#kokytosret", Command.KOKYTOSRET);
            _commandMap.Add("#infernoret", Command.INFERNORET);
            _commandMap.Add("#voidret", Command.VOIDRET);
            _commandMap.Add("#allret", Command.ALLRET);
            _commandMap.Add("#randomspell", Command.RANDOMSPELL);
            _commandMap.Add("#tainted", Command.TAINTED);
            _commandMap.Add("#forgebonus", Command.FORGEBONUS);
            _commandMap.Add("#fixforgebonus", Command.FIXFORGEBONUS);
            _commandMap.Add("#mastersmith", Command.MASTERSMITH);
            _commandMap.Add("#commaster", Command.COMMASTER);
            _commandMap.Add("#comslave", Command.COMSLAVE);
            _commandMap.Add("#indepspells", Command.INDEPSPELLS);
            _commandMap.Add("#fastcast", Command.FASTCAST);
            _commandMap.Add("#spellsinger", Command.SPELLSINGER);
            _commandMap.Add("#magicstudy", Command.MAGICSTUDY);
            _commandMap.Add("#bringeroffortune", Command.BRINGEROFFORTUNE);
            _commandMap.Add("#combatcaster", Command.COMBATCASTER);
            _commandMap.Add("#unsurr", Command.UNSURR);
            _commandMap.Add("#skirmisher", Command.SKIRMISHER);
            _commandMap.Add("#minsizeleader", Command.MINSIZELEADER);
            _commandMap.Add("#mcost", Command.MCOST);
            _commandMap.Add("#limitedregen", Command.LIMITEDREGEN);
            _commandMap.Add("#enchrebate10", Command.ENCHREBATE10);
            _commandMap.Add("#enchrebate20", Command.ENCHREBATE20);
            _commandMap.Add("#sailsize", Command.SAILSIZE);
            _commandMap.Add("#mindvessel", Command.MINDVESSEL);
            _commandMap.Add("#saltvul", Command.SALTVUL);
            _commandMap.Add("#shapechance", Command.SHAPECHANCE);
            _commandMap.Add("#notsacred", Command.NOTSACRED);
            _commandMap.Add("#appetite", Command.APPETITE);
            _commandMap.Add("#clearallspells", Command.CLEARALLSPELLS);
            _commandMap.Add("#selectspell", Command.SELECTSPELL);
            _commandMap.Add("#newspell", Command.NEWSPELL);
            _commandMap.Add("#copyspell", Command.COPYSPELL);
            _commandMap.Add("#details", Command.DETAILS);
            _commandMap.Add("#school", Command.SCHOOL);
            _commandMap.Add("#researchlevel", Command.RESEARCHLEVEL);
            _commandMap.Add("#path", Command.PATH);
            _commandMap.Add("#pathlevel", Command.PATHLEVEL);
            _commandMap.Add("#fatiguecost", Command.FATIGUECOST);
            _commandMap.Add("#damage", Command.DAMAGE);
            _commandMap.Add("#damagemon", Command.DAMAGEMON);
            _commandMap.Add("#nextspell", Command.NEXTSPELL);
            _commandMap.Add("#nextingeo", Command.NEXTINGEO);
            _commandMap.Add("#effect", Command.EFFECT);
            _commandMap.Add("#nreff", Command.NREFF);
            _commandMap.Add("#precision", Command.PRECISION);
            _commandMap.Add("#flightspr", Command.FLIGHTSPR);
            _commandMap.Add("#strikesound", Command.STRIKESOUND);
            _commandMap.Add("#provrange", Command.PROVRANGE);
            _commandMap.Add("#onlygeosrc", Command.ONLYGEOSRC);
            _commandMap.Add("#nogeosrc", Command.NOGEOSRC);
            _commandMap.Add("#onlygeodst", Command.ONLYGEODST);
            _commandMap.Add("#nogeodst", Command.NOGEODST);
            _commandMap.Add("#onlycoastsrc", Command.ONLYCOASTSRC);
            _commandMap.Add("#onlyatsite", Command.ONLYATSITE);
            _commandMap.Add("#onlyfriendlydst", Command.ONLYFRIENDLYDST);
            _commandMap.Add("#onlyowndst", Command.ONLYOWNDST);
            _commandMap.Add("#nowatertrace", Command.NOWATERTRACE);
            _commandMap.Add("#nolandtrace", Command.NOLANDTRACE);
            _commandMap.Add("#walkable", Command.WALKABLE);
            _commandMap.Add("#spec", Command.SPEC);
            _commandMap.Add("#restricted", Command.RESTRICTED);
            _commandMap.Add("#farsumcom", Command.FARSUMCOM);
            _commandMap.Add("#notfornation", Command.NOTFORNATION);
            _commandMap.Add("#casttime", Command.CASTTIME);
            _commandMap.Add("#godpathspell", Command.GODPATHSPELL);
            _commandMap.Add("#friendlyench", Command.FRIENDLYENCH);
            _commandMap.Add("#hiddenench", Command.HIDDENENCH);
            _commandMap.Add("#nocastmindless", Command.NOCASTMINDLESS);
            _commandMap.Add("#spellreqfly", Command.SPELLREQFLY);
            _commandMap.Add("#onlymnr", Command.ONLYMNR);
            _commandMap.Add("#notmnr", Command.NOTMNR);
            _commandMap.Add("#polygetmagic", Command.POLYGETMAGIC);
            _commandMap.Add("#maxbounces", Command.MAXBOUNCES);
            _commandMap.Add("#reqspellsinger", Command.REQSPELLSINGER);
            _commandMap.Add("#reqtaskmaster", Command.REQTASKMASTER);
            _commandMap.Add("#reqseduce", Command.REQSEDUCE);
            _commandMap.Add("#sethome", Command.SETHOME);
            _commandMap.Add("#reqsun", Command.REQSUN);
            _commandMap.Add("#ainocast", Command.AINOCAST);
            _commandMap.Add("#aibadlvl", Command.AIBADLVL);
            _commandMap.Add("#aispellmod", Command.AISPELLMOD);
            _commandMap.Add("#reqplant", Command.REQPLANT);
            _commandMap.Add("#reqnoplant", Command.REQNOPLANT);
            _commandMap.Add("#newitem", Command.NEWITEM);
            _commandMap.Add("#selectitem", Command.SELECTITEM);
            _commandMap.Add("#clearallitems", Command.CLEARALLITEMS);
            _commandMap.Add("#constlevel", Command.CONSTLEVEL);
            _commandMap.Add("#mainpath", Command.MAINPATH);
            _commandMap.Add("#mainlevel", Command.MAINLEVEL);
            _commandMap.Add("#secondarypath", Command.SECONDARYPATH);
            _commandMap.Add("#secondarylevel", Command.SECONDARYLEVEL);
            _commandMap.Add("#copyitem", Command.COPYITEM);
            _commandMap.Add("#spr", Command.SPR);
            _commandMap.Add("#pen", Command.PEN);
            _commandMap.Add("#spell", Command.SPELL);
            _commandMap.Add("#autospell", Command.AUTOSPELL);
            _commandMap.Add("#autospellrepeat", Command.AUTOSPELLREPEAT);
            _commandMap.Add("#luck", Command.LUCK);
            _commandMap.Add("#morale", Command.MORALE);
            _commandMap.Add("#quickness", Command.QUICKNESS);
            _commandMap.Add("#bless", Command.BLESS);
            _commandMap.Add("#barkskin", Command.BARKSKIN);
            _commandMap.Add("#stoneskin", Command.STONESKIN);
            _commandMap.Add("#ironskin", Command.IRONSKIN);
            _commandMap.Add("#bers", Command.BERS);
            _commandMap.Add("#extralife", Command.EXTRALIFE);
            _commandMap.Add("#polyimmune", Command.POLYIMMUNE);
            _commandMap.Add("#autobless", Command.AUTOBLESS);
            _commandMap.Add("#mapspeed", Command.MAPSPEED);
            _commandMap.Add("#waterbreathing", Command.WATERBREATHING);
            _commandMap.Add("#fly", Command.FLY);
            _commandMap.Add("#run", Command.RUN);
            _commandMap.Add("#sneakunit", Command.SNEAKUNIT);
            _commandMap.Add("#stealthboost", Command.STEALTHBOOST);
            _commandMap.Add("#swift", Command.SWIFT);
            _commandMap.Add("#reqeyes", Command.REQEYES);
            _commandMap.Add("#nofind", Command.NOFIND);
            _commandMap.Add("#restricteditem", Command.RESTRICTEDITEM);
            _commandMap.Add("#nationrebate", Command.NATIONREBATE);
            _commandMap.Add("#noforgebonus", Command.NOFORGEBONUS);
            _commandMap.Add("#islance", Command.ISLANCE);
            _commandMap.Add("#minsize", Command.MINSIZE);
            _commandMap.Add("#maxsize", Command.MAXSIZE);
            _commandMap.Add("#cursed", Command.CURSED);
            _commandMap.Add("#nomounted", Command.NOMOUNTED);
            _commandMap.Add("#curse", Command.CURSE);
            _commandMap.Add("#nocoldblood", Command.NOCOLDBLOOD);
            _commandMap.Add("#disease", Command.DISEASE);
            _commandMap.Add("#nodemon", Command.NODEMON);
            _commandMap.Add("#chestwound", Command.CHESTWOUND);
            _commandMap.Add("#noundead", Command.NOUNDEAD);
            _commandMap.Add("#noinanim", Command.NOINANIM);
            _commandMap.Add("#noimmobile", Command.NOIMMOBILE);
            _commandMap.Add("#feeblemind", Command.FEEBLEMIND);
            _commandMap.Add("#mute", Command.MUTE);
            _commandMap.Add("#onlymounted", Command.ONLYMOUNTED);
            _commandMap.Add("#onlycoldblood", Command.ONLYCOLDBLOOD);
            _commandMap.Add("#nhwound", Command.NHWOUND);
            _commandMap.Add("#onlydemon", Command.ONLYDEMON);
            _commandMap.Add("#crippled", Command.CRIPPLED);
            _commandMap.Add("#onlyundead", Command.ONLYUNDEAD);
            _commandMap.Add("#loseeye", Command.LOSEEYE);
            _commandMap.Add("#onlyinanim", Command.ONLYINANIM);
            _commandMap.Add("#onlyimmobile", Command.ONLYIMMOBILE);
            _commandMap.Add("#recuperation", Command.RECUPERATION);
            _commandMap.Add("#yearaging", Command.YEARAGING);
            _commandMap.Add("#noaging", Command.NOAGING);
            _commandMap.Add("#noagingland", Command.NOAGINGLAND);
            _commandMap.Add("#itemcost1", Command.ITEMCOST1);
            _commandMap.Add("#itemcost2", Command.ITEMCOST2);
            _commandMap.Add("#itemdrawsize", Command.ITEMDRAWSIZE);
            _commandMap.Add("#champprize", Command.CHAMPPRIZE);
            _commandMap.Add("#tmpbloodslaves", Command.TMPBLOODSLAVES);
            _commandMap.Add("#selectnametype", Command.SELECTNAMETYPE);
            _commandMap.Add("#addname", Command.ADDNAME);
            _commandMap.Add("#newmerc", Command.NEWMERC);
            _commandMap.Add("#clearmercs", Command.CLEARMERCS);
            _commandMap.Add("#level", Command.LEVEL);
            _commandMap.Add("#bossname", Command.BOSSNAME);
            _commandMap.Add("#com", Command.COM);
            _commandMap.Add("#unit", Command.UNIT);
            _commandMap.Add("#nrunits", Command.NRUNITS);
            _commandMap.Add("#minmen", Command.MINMEN);
            _commandMap.Add("#minpay", Command.MINPAY);
            _commandMap.Add("#xp", Command.XP);
            _commandMap.Add("#randequip", Command.RANDEQUIP);
            _commandMap.Add("#recrate", Command.RECRATE);
            _commandMap.Add("#item", Command.ITEM);
            _commandMap.Add("#eramask", Command.ERAMASK);
            _commandMap.Add("#selectsite", Command.SELECTSITE);
            _commandMap.Add("#newsite", Command.NEWSITE);
            _commandMap.Add("#loc", Command.LOC);
            _commandMap.Add("#gems", Command.GEMS);
            _commandMap.Add("#res", Command.RES);
            _commandMap.Add("#rarity", Command.RARITY);
            _commandMap.Add("#decunrest", Command.DECUNREST);
            _commandMap.Add("#supply", Command.SUPPLY);
            _commandMap.Add("#homemon", Command.HOMEMON);
            _commandMap.Add("#homecom", Command.HOMECOM);
            _commandMap.Add("#mon", Command.MON);
            _commandMap.Add("#nat", Command.NAT);
            _commandMap.Add("#natmon", Command.NATMON);
            _commandMap.Add("#natcom", Command.NATCOM);
            _commandMap.Add("#summon", Command.SUMMON);
            _commandMap.Add("#summonlvl2", Command.SUMMONLVL2);
            _commandMap.Add("#summonlvl3", Command.SUMMONLVL3);
            _commandMap.Add("#summonlvl4", Command.SUMMONLVL4);
            _commandMap.Add("#voidgate", Command.VOIDGATE);
            _commandMap.Add("#wallcom", Command.WALLCOM);
            _commandMap.Add("#wallunit", Command.WALLUNIT);
            _commandMap.Add("#wallmult", Command.WALLMULT);
            _commandMap.Add("#uwdefcom1", Command.UWDEFCOM1);
            _commandMap.Add("#uwdefcom2", Command.UWDEFCOM2);
            _commandMap.Add("#uwdefunit1", Command.UWDEFUNIT1);
            _commandMap.Add("#uwdefmult1", Command.UWDEFMULT1);
            _commandMap.Add("#uwdefunit1b", Command.UWDEFUNIT1B);
            _commandMap.Add("#uwdefmult1b", Command.UWDEFMULT1B);
            _commandMap.Add("#uwdefunit2", Command.UWDEFUNIT2);
            _commandMap.Add("#uwdefmult2", Command.UWDEFMULT2);
            _commandMap.Add("#uwdefunit2b", Command.UWDEFUNIT2B);
            _commandMap.Add("#uwdefmult2b", Command.UWDEFMULT2B);
            _commandMap.Add("#uwdefunit1c", Command.UWDEFUNIT1C);
            _commandMap.Add("#uwdefmult1c", Command.UWDEFMULT1C);
            _commandMap.Add("#uwdefunit1d", Command.UWDEFUNIT1D);
            _commandMap.Add("#uwdefmult1d", Command.UWDEFMULT1D);
            _commandMap.Add("#uwwallunit", Command.UWWALLUNIT);
            _commandMap.Add("#uwwallmult", Command.UWWALLMULT);
            _commandMap.Add("#uwwallcom", Command.UWWALLCOM);
            _commandMap.Add("#conjcost", Command.CONJCOST);
            _commandMap.Add("#altcost", Command.ALTCOST);
            _commandMap.Add("#evocost", Command.EVOCOST);
            _commandMap.Add("#constcost", Command.CONSTCOST);
            _commandMap.Add("#enchcost", Command.ENCHCOST);
            _commandMap.Add("#thaucost", Command.THAUCOST);
            _commandMap.Add("#bloodcost", Command.BLOODCOST);
            _commandMap.Add("#scry", Command.SCRY);
            _commandMap.Add("#scryrange", Command.SCRYRANGE);
            _commandMap.Add("#cluster", Command.CLUSTER);
            _commandMap.Add("#holyfire", Command.HOLYFIRE);
            _commandMap.Add("#holypower", Command.HOLYPOWER);
            _commandMap.Add("#adventureruin", Command.ADVENTURERUIN);
            _commandMap.Add("#lab", Command.LAB);
            _commandMap.Add("#temple", Command.TEMPLE);
            _commandMap.Add("#fort", Command.FORT);
            _commandMap.Add("#claim", Command.CLAIM);
            _commandMap.Add("#dominion", Command.DOMINION);
            _commandMap.Add("#goddomchaos", Command.GODDOMCHAOS);
            _commandMap.Add("#goddomlazy", Command.GODDOMLAZY);
            _commandMap.Add("#goddomcold", Command.GODDOMCOLD);
            _commandMap.Add("#goddomdeath", Command.GODDOMDEATH);
            _commandMap.Add("#goddommisfortune", Command.GODDOMMISFORTUNE);
            _commandMap.Add("#goddomdrain", Command.GODDOMDRAIN);
            _commandMap.Add("#blesshp", Command.BLESSHP);
            _commandMap.Add("#blessanimawe", Command.BLESSANIMAWE);
            _commandMap.Add("#blessmr", Command.BLESSMR);
            _commandMap.Add("#blessawe", Command.BLESSAWE);
            _commandMap.Add("#blessmor", Command.BLESSMOR);
            _commandMap.Add("#blessstr", Command.BLESSSTR);
            _commandMap.Add("#blessdarkvis", Command.BLESSDARKVIS);
            _commandMap.Add("#blessatt", Command.BLESSATT);
            _commandMap.Add("#evil", Command.EVIL);
            _commandMap.Add("#blessdef", Command.BLESSDEF);
            _commandMap.Add("#blessprec", Command.BLESSPREC);
            _commandMap.Add("#blessfireres", Command.BLESSFIRERES);
            _commandMap.Add("#blesscoldres", Command.BLESSCOLDRES);
            _commandMap.Add("#blessshockres", Command.BLESSSHOCKRES);
            _commandMap.Add("#blesspoisres", Command.BLESSPOISRES);
            _commandMap.Add("#blessairshld", Command.BLESSAIRSHLD);
            _commandMap.Add("#blessreinvig", Command.BLESSREINVIG);
            _commandMap.Add("#blessdtv", Command.BLESSDTV);
            _commandMap.Add("#wild", Command.WILD);
            _commandMap.Add("#recallgod", Command.RECALLGOD);
            _commandMap.Add("#domwar", Command.DOMWAR);
            _commandMap.Add("#minegold", Command.MINEGOLD);
            _commandMap.Add("#indepflag", Command.INDEPFLAG);
            _commandMap.Add("#selectnation", Command.SELECTNATION);
            _commandMap.Add("#newnation", Command.NEWNATION);
            _commandMap.Add("#clearnation", Command.CLEARNATION);
            _commandMap.Add("#epithet", Command.EPITHET);
            _commandMap.Add("#era", Command.ERA);
            _commandMap.Add("#summary", Command.SUMMARY);
            _commandMap.Add("#brief", Command.BRIEF);
            _commandMap.Add("#color", Command.COLOR);
            _commandMap.Add("#secondarycolor", Command.SECONDARYCOLOR);
            _commandMap.Add("#flag", Command.FLAG);
            _commandMap.Add("#disableoldnations", Command.DISABLEOLDNATIONS);
            _commandMap.Add("#clearsites", Command.CLEARSITES);
            _commandMap.Add("#startsite", Command.STARTSITE);
            _commandMap.Add("#islandsite", Command.ISLANDSITE);
            _commandMap.Add("#likesterr", Command.LIKESTERR);
            _commandMap.Add("#idealcold", Command.IDEALCOLD);
            _commandMap.Add("#defchaos", Command.DEFCHAOS);
            _commandMap.Add("#defsloth", Command.DEFSLOTH);
            _commandMap.Add("#defdeath", Command.DEFDEATH);
            _commandMap.Add("#defmisfortune", Command.DEFMISFORTUNE);
            _commandMap.Add("#defdrain", Command.DEFDRAIN);
            _commandMap.Add("#uwnation", Command.UWNATION);
            _commandMap.Add("#coastnation", Command.COASTNATION);
            _commandMap.Add("#riverstart", Command.RIVERSTART);
            _commandMap.Add("#cavenation", Command.CAVENATION);
            _commandMap.Add("#islandnation", Command.ISLANDNATION);
            _commandMap.Add("#hatesterr", Command.HATESTERR);
            _commandMap.Add("#killcappop", Command.KILLCAPPOP);
            _commandMap.Add("#aiholdgod", Command.AIHOLDGOD);
            _commandMap.Add("#aiawake", Command.AIAWAKE);
            _commandMap.Add("#aifirenation", Command.AIFIRENATION);
            _commandMap.Add("#aiairnation", Command.AIAIRNATION);
            _commandMap.Add("#aiwaternation", Command.AIWATERNATION);
            _commandMap.Add("#aiearthnation", Command.AIEARTHNATION);
            _commandMap.Add("#ainaturenation", Command.AINATURENATION);
            _commandMap.Add("#aiastralnation", Command.AIASTRALNATION);
            _commandMap.Add("#aideathnation", Command.AIDEATHNATION);
            _commandMap.Add("#aibloodnation", Command.AIBLOODNATION);
            _commandMap.Add("#bloodnation", Command.BLOODNATION);
            _commandMap.Add("#aigoodbless", Command.AIGOODBLESS);
            _commandMap.Add("#aimusthavemag", Command.AIMUSTHAVEMAG);
            _commandMap.Add("#aicheapholy", Command.AICHEAPHOLY);
            _commandMap.Add("#aiholyranged", Command.AIHOLYRANGED);
            _commandMap.Add("#aiheavyrec", Command.AIHEAVYREC);
            _commandMap.Add("#aimagerec", Command.AIMAGEREC);
            _commandMap.Add("#clearrec", Command.CLEARREC);
            _commandMap.Add("#startcom", Command.STARTCOM);
            _commandMap.Add("#coastcom1", Command.COASTCOM1);
            _commandMap.Add("#coastcom2", Command.COASTCOM2);
            _commandMap.Add("#addforeignunit", Command.ADDFOREIGNUNIT);
            _commandMap.Add("#addforeigncom", Command.ADDFOREIGNCOM);
            _commandMap.Add("#forestrec", Command.FORESTREC);
            _commandMap.Add("#mountainrec", Command.MOUNTAINREC);
            _commandMap.Add("#swamprec", Command.SWAMPREC);
            _commandMap.Add("#wasterec", Command.WASTEREC);
            _commandMap.Add("#caverec", Command.CAVEREC);
            _commandMap.Add("#coastrec", Command.COASTREC);
            _commandMap.Add("#forestcom", Command.FORESTCOM);
            _commandMap.Add("#mountaincom", Command.MOUNTAINCOM);
            _commandMap.Add("#swampcom", Command.SWAMPCOM);
            _commandMap.Add("#wastecom", Command.WASTECOM);
            _commandMap.Add("#cavecom", Command.CAVECOM);
            _commandMap.Add("#coastcom", Command.COASTCOM);
            _commandMap.Add("#startscout", Command.STARTSCOUT);
            _commandMap.Add("#startunittype1", Command.STARTUNITTYPE1);
            _commandMap.Add("#startunitnbrs1", Command.STARTUNITNBRS1);
            _commandMap.Add("#startunittype2", Command.STARTUNITTYPE2);
            _commandMap.Add("#startunitnbrs2", Command.STARTUNITNBRS2);
            _commandMap.Add("#addrecunit", Command.ADDRECUNIT);
            _commandMap.Add("#addreccom", Command.ADDRECCOM);
            _commandMap.Add("#uwrec", Command.UWREC);
            _commandMap.Add("#uwcom", Command.UWCOM);
            _commandMap.Add("#coastunit1", Command.COASTUNIT1);
            _commandMap.Add("#coastunit2", Command.COASTUNIT2);
            _commandMap.Add("#coastunit3", Command.COASTUNIT3);
            _commandMap.Add("#landrec", Command.LANDREC);
            _commandMap.Add("#landcom", Command.LANDCOM);
            _commandMap.Add("#merccost", Command.MERCCOST);
            _commandMap.Add("#hero1", Command.HERO1);
            _commandMap.Add("#hero2", Command.HERO2);
            _commandMap.Add("#hero3", Command.HERO3);
            _commandMap.Add("#hero4", Command.HERO4);
            _commandMap.Add("#hero5", Command.HERO5);
            _commandMap.Add("#hero6", Command.HERO6);
            _commandMap.Add("#hero7", Command.HERO7);
            _commandMap.Add("#hero8", Command.HERO8);
            _commandMap.Add("#hero9", Command.HERO9);
            _commandMap.Add("#hero10", Command.HERO10);
            _commandMap.Add("#multihero1", Command.MULTIHERO1);
            _commandMap.Add("#multihero2", Command.MULTIHERO2);
            _commandMap.Add("#multihero3", Command.MULTIHERO3);
            _commandMap.Add("#multihero4", Command.MULTIHERO4);
            _commandMap.Add("#multihero5", Command.MULTIHERO5);
            _commandMap.Add("#multihero6", Command.MULTIHERO6);
            _commandMap.Add("#multihero7", Command.MULTIHERO7);
            _commandMap.Add("#noforeignrec", Command.NOFOREIGNREC);
            _commandMap.Add("#defcom1", Command.DEFCOM1);
            _commandMap.Add("#defcom2", Command.DEFCOM2);
            _commandMap.Add("#defunit1", Command.DEFUNIT1);
            _commandMap.Add("#defunit1b", Command.DEFUNIT1B);
            _commandMap.Add("#defunit1c", Command.DEFUNIT1C);
            _commandMap.Add("#defunit1d", Command.DEFUNIT1D);
            _commandMap.Add("#defunit2", Command.DEFUNIT2);
            _commandMap.Add("#defunit2b", Command.DEFUNIT2B);
            _commandMap.Add("#defmult1", Command.DEFMULT1);
            _commandMap.Add("#defmult1b", Command.DEFMULT1B);
            _commandMap.Add("#defmult1c", Command.DEFMULT1C);
            _commandMap.Add("#defmult1d", Command.DEFMULT1D);
            _commandMap.Add("#defmult2", Command.DEFMULT2);
            _commandMap.Add("#defmult2b", Command.DEFMULT2B);
            _commandMap.Add("#badindpd", Command.BADINDPD);
            _commandMap.Add("#cleargods", Command.CLEARGODS);
            _commandMap.Add("#addgod", Command.ADDGOD);
            _commandMap.Add("#noundeadgods", Command.NOUNDEADGODS);
            _commandMap.Add("#delgod", Command.DELGOD);
            _commandMap.Add("#likespop", Command.LIKESPOP);
            _commandMap.Add("#godrebirth", Command.GODREBIRTH);
            _commandMap.Add("#cheapgod20", Command.CHEAPGOD20);
            _commandMap.Add("#uwbuild", Command.UWBUILD);
            _commandMap.Add("#cheapgod40", Command.CHEAPGOD40);
            _commandMap.Add("#fireblessbonus", Command.FIREBLESSBONUS);
            _commandMap.Add("#airblessbonus", Command.AIRBLESSBONUS);
            _commandMap.Add("#waterblessbonus", Command.WATERBLESSBONUS);
            _commandMap.Add("#earthblessbonus", Command.EARTHBLESSBONUS);
            _commandMap.Add("#astralblessbonus", Command.ASTRALBLESSBONUS);
            _commandMap.Add("#deathblessbonus", Command.DEATHBLESSBONUS);
            _commandMap.Add("#natureblessbonus", Command.NATUREBLESSBONUS);
            _commandMap.Add("#bloodblessbonus", Command.BLOODBLESSBONUS);
            _commandMap.Add("#fortera", Command.FORTERA);
            _commandMap.Add("#fortcost", Command.FORTCOST);
            _commandMap.Add("#labcost", Command.LABCOST);
            _commandMap.Add("#templecost", Command.TEMPLECOST);
            _commandMap.Add("#forestlabcost", Command.FORESTLABCOST);
            _commandMap.Add("#foresttemplecost", Command.FORESTTEMPLECOST);
            _commandMap.Add("#templepic", Command.TEMPLEPIC);
            _commandMap.Add("#templegems", Command.TEMPLEGEMS);
            _commandMap.Add("#homefort", Command.HOMEFORT);
            _commandMap.Add("#buildfort", Command.BUILDFORT);
            _commandMap.Add("#builduwfort", Command.BUILDUWFORT);
            _commandMap.Add("#buildcoastfort", Command.BUILDCOASTFORT);
            _commandMap.Add("#fortunrest", Command.FORTUNREST);
            _commandMap.Add("#nodeathsupply", Command.NODEATHSUPPLY);
            _commandMap.Add("#halfdeathinc", Command.HALFDEATHINC);
            _commandMap.Add("#halfdeathpop", Command.HALFDEATHPOP);
            _commandMap.Add("#domdeathsense", Command.DOMDEATHSENSE);
            _commandMap.Add("#nationinc", Command.NATIONINC);
            _commandMap.Add("#castleprod", Command.CASTLEPROD);
            _commandMap.Add("#tradecoast", Command.TRADECOAST);
            _commandMap.Add("#golemhp", Command.GOLEMHP);
            _commandMap.Add("#spreadcold", Command.SPREADCOLD);
            _commandMap.Add("#spreadheat", Command.SPREADHEAT);
            _commandMap.Add("#spreadchaos", Command.SPREADCHAOS);
            _commandMap.Add("#spreadlazy", Command.SPREADLAZY);
            _commandMap.Add("#spreaddeath", Command.SPREADDEATH);
            _commandMap.Add("#nopreach", Command.NOPREACH);
            _commandMap.Add("#dyingdom", Command.DYINGDOM);
            _commandMap.Add("#sacrificedom", Command.SACRIFICEDOM);
            _commandMap.Add("#domkill", Command.DOMKILL);
            _commandMap.Add("#domunrest", Command.DOMUNREST);
            _commandMap.Add("#autoundead", Command.AUTOUNDEAD);
            _commandMap.Add("#guardspirit", Command.GUARDSPIRIT);
            _commandMap.Add("#syncretism", Command.SYNCRETISM);
            _commandMap.Add("#domsail", Command.DOMSAIL);
            _commandMap.Add("#priestreanim", Command.PRIESTREANIM);
            _commandMap.Add("#undeadreanim", Command.UNDEADREANIM);
            _commandMap.Add("#horsereanim", Command.HORSEREANIM);
            _commandMap.Add("#wightreanim", Command.WIGHTREANIM);
            _commandMap.Add("#tombwyrmreanim", Command.TOMBWYRMREANIM);
            _commandMap.Add("#manikinreanim", Command.MANIKINREANIM);
            _commandMap.Add("#supayareanim", Command.SUPAYAREANIM);
            _commandMap.Add("#greekreanim", Command.GREEKREANIM);
            _commandMap.Add("#ghostreanim", Command.GHOSTREANIM);
            _commandMap.Add("#selectpoptype", Command.SELECTPOPTYPE);
            _commandMap.Add("#cleardef", Command.CLEARDEF);
            _commandMap.Add("#poppergold", Command.POPPERGOLD);
            _commandMap.Add("#resourcemult", Command.RESOURCEMULT);
            _commandMap.Add("#supplymult", Command.SUPPLYMULT);
            _commandMap.Add("#unresthalfinc", Command.UNRESTHALFINC);
            _commandMap.Add("#unresthalfres", Command.UNRESTHALFRES);
            _commandMap.Add("#eventisrare", Command.EVENTISRARE);
            _commandMap.Add("#turmoilincome", Command.TURMOILINCOME);
            _commandMap.Add("#turmoilevents", Command.TURMOILEVENTS);
            _commandMap.Add("#deathincome", Command.DEATHINCOME);
            _commandMap.Add("#deathsupply", Command.DEATHSUPPLY);
            _commandMap.Add("#deathdeath", Command.DEATHDEATH);
            _commandMap.Add("#slothincome", Command.SLOTHINCOME);
            _commandMap.Add("#slothresources", Command.SLOTHRESOURCES);
            _commandMap.Add("#coldincome", Command.COLDINCOME);
            _commandMap.Add("#coldsupply", Command.COLDSUPPLY);
            _commandMap.Add("#tempscalecap", Command.TEMPSCALECAP);
            _commandMap.Add("#misfortune", Command.MISFORTUNE);
            _commandMap.Add("#luckevents", Command.LUCKEVENTS);
            _commandMap.Add("#researchscale", Command.RESEARCHSCALE);
            _commandMap.Add("#cavelabcost", Command.CAVELABCOST);
            _commandMap.Add("#cavetemplecost", Command.CAVETEMPLECOST);
            _commandMap.Add("#swamplabcost", Command.SWAMPLABCOST);
            _commandMap.Add("#swamptemplecost", Command.SWAMPTEMPLECOST);
            _commandMap.Add("#mountlabcost", Command.MOUNTLABCOST);
            _commandMap.Add("#mounttemplecost", Command.MOUNTTEMPLECOST);
            _commandMap.Add("#wastelabcost", Command.WASTELABCOST);
            _commandMap.Add("#wastetemplecost", Command.WASTETEMPLECOST);
            _commandMap.Add("#futuresite", Command.FUTURESITE);
            _commandMap.Add("#clearallevents", Command.CLEARALLEVENTS);
            _commandMap.Add("#newevent", Command.NEWEVENT);
            _commandMap.Add("#req_rare", Command.REQ_RARE);
            _commandMap.Add("#req_unique", Command.REQ_UNIQUE);
            _commandMap.Add("#req_story", Command.REQ_STORY);
            _commandMap.Add("#req_indepok", Command.REQ_INDEPOK);
            _commandMap.Add("#req_era", Command.REQ_ERA);
            _commandMap.Add("#req_noera", Command.REQ_NOERA);
            _commandMap.Add("#req_turn", Command.REQ_TURN);
            _commandMap.Add("#req_maxturn", Command.REQ_MAXTURN);
            _commandMap.Add("#req_pregame", Command.REQ_PREGAME);
            _commandMap.Add("#req_season", Command.REQ_SEASON);
            _commandMap.Add("#req_noseason", Command.REQ_NOSEASON);
            _commandMap.Add("#req_nation", Command.REQ_NATION);
            _commandMap.Add("#req_nonation", Command.REQ_NONATION);
            _commandMap.Add("#req_fornation", Command.REQ_FORNATION);
            _commandMap.Add("#req_notnation", Command.REQ_NOTNATION);
            _commandMap.Add("#req_notfornation", Command.REQ_NOTFORNATION);
            _commandMap.Add("#req_notforally", Command.REQ_NOTFORALLY);
            _commandMap.Add("#req_gem", Command.REQ_GEM);
            _commandMap.Add("#req_gold", Command.REQ_GOLD);
            _commandMap.Add("#req_capital", Command.REQ_CAPITAL);
            _commandMap.Add("#req_owncapital", Command.REQ_OWNCAPITAL);
            _commandMap.Add("#req_poptype", Command.REQ_POPTYPE);
            _commandMap.Add("#req_pop0ok", Command.REQ_POP0OK);
            _commandMap.Add("#req_maxpop", Command.REQ_MAXPOP);
            _commandMap.Add("#req_minpop", Command.REQ_MINPOP);
            _commandMap.Add("#req_mindef", Command.REQ_MINDEF);
            _commandMap.Add("#req_maxdef", Command.REQ_MAXDEF);
            _commandMap.Add("#req_minunrest", Command.REQ_MINUNREST);
            _commandMap.Add("#req_maxunrest", Command.REQ_MAXUNREST);
            _commandMap.Add("#req_lab", Command.REQ_LAB);
            _commandMap.Add("#req_temple", Command.REQ_TEMPLE);
            _commandMap.Add("#req_fort", Command.REQ_FORT);
            _commandMap.Add("#req_land", Command.REQ_LAND);
            _commandMap.Add("#req_coast", Command.REQ_COAST);
            _commandMap.Add("#req_mountain", Command.REQ_MOUNTAIN);
            _commandMap.Add("#req_forest", Command.REQ_FOREST);
            _commandMap.Add("#req_farm", Command.REQ_FARM);
            _commandMap.Add("#req_swamp", Command.REQ_SWAMP);
            _commandMap.Add("#req_waste", Command.REQ_WASTE);
            _commandMap.Add("#req_cave", Command.REQ_CAVE);
            _commandMap.Add("#req_freshwater", Command.REQ_FRESHWATER);
            _commandMap.Add("#req_freesites", Command.REQ_FREESITES);
            _commandMap.Add("#req_nositenbr", Command.REQ_NOSITENBR);
            _commandMap.Add("#req_foundsite", Command.REQ_FOUNDSITE);
            _commandMap.Add("#req_hiddensite", Command.REQ_HIDDENSITE);
            _commandMap.Add("#req_site", Command.REQ_SITE);
            _commandMap.Add("#req_nearbysite", Command.REQ_NEARBYSITE);
            _commandMap.Add("#req_claimedthrone", Command.REQ_CLAIMEDTHRONE);
            _commandMap.Add("#req_unclaimedthrone", Command.REQ_UNCLAIMEDTHRONE);
            _commandMap.Add("#req_fullowner", Command.REQ_FULLOWNER);
            _commandMap.Add("#req_domowner", Command.REQ_DOMOWNER);
            _commandMap.Add("#req_mydominion", Command.REQ_MYDOMINION);
            _commandMap.Add("#req_dominion", Command.REQ_DOMINION);
            _commandMap.Add("#req_maxdominion", Command.REQ_MAXDOMINION);
            _commandMap.Add("#req_domchance", Command.REQ_DOMCHANCE);
            _commandMap.Add("#req_godismnr", Command.REQ_GODISMNR);
            _commandMap.Add("#req_chaos", Command.REQ_CHAOS);
            _commandMap.Add("#req_lazy", Command.REQ_LAZY);
            _commandMap.Add("#req_cold", Command.REQ_COLD);
            _commandMap.Add("#req_death", Command.REQ_DEATH);
            _commandMap.Add("#req_unluck", Command.REQ_UNLUCK);
            _commandMap.Add("#req_unmagic", Command.REQ_UNMAGIC);
            _commandMap.Add("#req_order", Command.REQ_ORDER);
            _commandMap.Add("#req_prod", Command.REQ_PROD);
            _commandMap.Add("#req_heat", Command.REQ_HEAT);
            _commandMap.Add("#req_growth", Command.REQ_GROWTH);
            _commandMap.Add("#req_luck", Command.REQ_LUCK);
            _commandMap.Add("#req_magic", Command.REQ_MAGIC);
            _commandMap.Add("#req_commander", Command.REQ_COMMANDER);
            _commandMap.Add("#req_monster", Command.REQ_MONSTER);
            _commandMap.Add("#req_2monsters", Command.REQ_2MONSTERS);
            _commandMap.Add("#req_5monsters", Command.REQ_5MONSTERS);
            _commandMap.Add("#req_nomonster", Command.REQ_NOMONSTER);
            _commandMap.Add("#req_mnr", Command.REQ_MNR);
            _commandMap.Add("#req_nomnr", Command.REQ_NOMNR);
            _commandMap.Add("#req_deadmnr", Command.REQ_DEADMNR);
            _commandMap.Add("#req_mintroops", Command.REQ_MINTROOPS);
            _commandMap.Add("#req_maxtroops", Command.REQ_MAXTROOPS);
            _commandMap.Add("#req_humanoidres", Command.REQ_HUMANOIDRES);
            _commandMap.Add("#req_researcher", Command.REQ_RESEARCHER);
            _commandMap.Add("#req_preach", Command.REQ_PREACH);
            _commandMap.Add("#req_pathfire", Command.REQ_PATHFIRE);
            _commandMap.Add("#req_pathair", Command.REQ_PATHAIR);
            _commandMap.Add("#req_pathwater", Command.REQ_PATHWATER);
            _commandMap.Add("#req_pathearth", Command.REQ_PATHEARTH);
            _commandMap.Add("#req_pathastral", Command.REQ_PATHASTRAL);
            _commandMap.Add("#req_pathdeath", Command.REQ_PATHDEATH);
            _commandMap.Add("#req_pathnature", Command.REQ_PATHNATURE);
            _commandMap.Add("#req_pathblood", Command.REQ_PATHBLOOD);
            _commandMap.Add("#req_pathholy", Command.REQ_PATHHOLY);
            _commandMap.Add("#req_nopathfire", Command.REQ_NOPATHFIRE);
            _commandMap.Add("#req_nopathair", Command.REQ_NOPATHAIR);
            _commandMap.Add("#req_nopathwater", Command.REQ_NOPATHWATER);
            _commandMap.Add("#req_nopathearth", Command.REQ_NOPATHEARTH);
            _commandMap.Add("#req_nopathastral", Command.REQ_NOPATHASTRAL);
            _commandMap.Add("#req_nopathdeath", Command.REQ_NOPATHDEATH);
            _commandMap.Add("#req_nopathnature", Command.REQ_NOPATHNATURE);
            _commandMap.Add("#req_nopathblood", Command.REQ_NOPATHBLOOD);
            _commandMap.Add("#req_nopathholy", Command.REQ_NOPATHHOLY);
            _commandMap.Add("#req_nopathall", Command.REQ_NOPATHALL);
            _commandMap.Add("#req_targmnr", Command.REQ_TARGMNR);
            _commandMap.Add("#req_targnomnr", Command.REQ_TARGNOMNR);
            _commandMap.Add("#req_targgod", Command.REQ_TARGGOD);
            _commandMap.Add("#req_targprophet", Command.REQ_TARGPROPHET);
            _commandMap.Add("#req_targhumanoid", Command.REQ_TARGHUMANOID);
            _commandMap.Add("#req_targmale", Command.REQ_TARGMALE);
            _commandMap.Add("#req_targpath1", Command.REQ_TARGPATH1);
            _commandMap.Add("#req_targpath2", Command.REQ_TARGPATH2);
            _commandMap.Add("#req_targpath3", Command.REQ_TARGPATH3);
            _commandMap.Add("#req_targpath4", Command.REQ_TARGPATH4);
            _commandMap.Add("#req_targnopath1", Command.REQ_TARGNOPATH1);
            _commandMap.Add("#req_targnopath2", Command.REQ_TARGNOPATH2);
            _commandMap.Add("#req_targnopath3", Command.REQ_TARGNOPATH3);
            _commandMap.Add("#req_targnopath4", Command.REQ_TARGNOPATH4);
            _commandMap.Add("#req_targaff", Command.REQ_TARGAFF);
            _commandMap.Add("#req_targorder", Command.REQ_TARGORDER);
            _commandMap.Add("#req_targitem", Command.REQ_TARGITEM);
            _commandMap.Add("#req_targnoitem", Command.REQ_TARGNOITEM);
            _commandMap.Add("#req_targundead", Command.REQ_TARGUNDEAD);
            _commandMap.Add("#req_targdemon", Command.REQ_TARGDEMON);
            _commandMap.Add("#req_targanimal", Command.REQ_TARGANIMAL);
            _commandMap.Add("#req_targinanimate", Command.REQ_TARGINANIMATE);
            _commandMap.Add("#req_targmindless", Command.REQ_TARGMINDLESS);
            _commandMap.Add("#req_targimmobile", Command.REQ_TARGIMMOBILE);
            _commandMap.Add("#req_targmagicbeing", Command.REQ_TARGMAGICBEING);
            _commandMap.Add("#req_targowner", Command.REQ_TARGOWNER);
            _commandMap.Add("#req_targforeignok", Command.REQ_TARGFOREIGNOK);
            _commandMap.Add("#req_targminsize", Command.REQ_TARGMINSIZE);
            _commandMap.Add("#req_targmaxsize", Command.REQ_TARGMAXSIZE);
            _commandMap.Add("#req_code", Command.REQ_CODE);
            _commandMap.Add("#req_anycode", Command.REQ_ANYCODE);
            _commandMap.Add("#req_notanycode", Command.REQ_NOTANYCODE);
            _commandMap.Add("#req_nearbycode", Command.REQ_NEARBYCODE);
            _commandMap.Add("#req_nearowncode", Command.REQ_NEAROWNCODE);
            _commandMap.Add("#req_permonth", Command.REQ_PERMONTH);
            _commandMap.Add("#req_noench", Command.REQ_NOENCH);
            _commandMap.Add("#req_ench", Command.REQ_ENCH);
            _commandMap.Add("#req_myench", Command.REQ_MYENCH);
            _commandMap.Add("#req_friendlyench", Command.REQ_FRIENDLYENCH);
            _commandMap.Add("#req_hostileench", Command.REQ_HOSTILEENCH);
            _commandMap.Add("#req_enchdom", Command.REQ_ENCHDOM);
            _commandMap.Add("#req_arenadone", Command.REQ_ARENADONE);
            _commandMap.Add("#nation", Command.NATION);
            _commandMap.Add("#nationench", Command.NATIONENCH);
            _commandMap.Add("#msg", Command.MSG);
            _commandMap.Add("#notext", Command.NOTEXT);
            _commandMap.Add("#nolog", Command.NOLOG);
            _commandMap.Add("#magicitem", Command.MAGICITEM);
            _commandMap.Add("#exactgold", Command.EXACTGOLD);
            _commandMap.Add("#incscale2", Command.INCSCALE2);
            _commandMap.Add("#incscale3", Command.INCSCALE3);
            _commandMap.Add("#1d3vis", Command.ZZ1D3VIS);
            _commandMap.Add("#1d6vis", Command.ZZ1D6VIS);
            _commandMap.Add("#2d4vis", Command.ZZ2D4VIS);
            _commandMap.Add("#2d6vis", Command.ZZ2D6VIS);
            _commandMap.Add("#3d6vis", Command.ZZ3D6VIS);
            _commandMap.Add("#decscale2", Command.DECSCALE2);
            _commandMap.Add("#decscale3", Command.DECSCALE3);
            _commandMap.Add("#4d6vis", Command.ZZ4D6VIS);
            _commandMap.Add("#gemloss", Command.GEMLOSS);
            _commandMap.Add("#landgold", Command.LANDGOLD);
            _commandMap.Add("#landprod", Command.LANDPROD);
            _commandMap.Add("#taxboost", Command.TAXBOOST);
            _commandMap.Add("#defence", Command.DEFENCE);
            _commandMap.Add("#kill", Command.KILL);
            _commandMap.Add("#killpop", Command.KILLPOP);
            _commandMap.Add("#incpop", Command.INCPOP);
            _commandMap.Add("#inccorpses", Command.INCCORPSES);
            _commandMap.Add("#emigration", Command.EMIGRATION);
            _commandMap.Add("#unrest", Command.UNREST);
            _commandMap.Add("#incdom", Command.INCDOM);
            _commandMap.Add("#revealsite", Command.REVEALSITE);
            _commandMap.Add("#addsite", Command.ADDSITE);
            _commandMap.Add("#removesite", Command.REMOVESITE);
            _commandMap.Add("#hiddensite", Command.HIDDENSITE);
            _commandMap.Add("#visitors", Command.VISITORS);
            _commandMap.Add("#newdom", Command.NEWDOM);
            _commandMap.Add("#revolt", Command.REVOLT);
            _commandMap.Add("#revealprov", Command.REVEALPROV);
            _commandMap.Add("#claimthrone", Command.CLAIMTHRONE);
            _commandMap.Add("#assowner", Command.ASSOWNER);
            _commandMap.Add("#stealthcom", Command.STEALTHCOM);
            _commandMap.Add("#2com", Command.ZZ2COM);
            _commandMap.Add("#4com", Command.ZZ4COM);
            _commandMap.Add("#5com", Command.ZZ5COM);
            _commandMap.Add("#tempunits", Command.TEMPUNITS);
            _commandMap.Add("#1unit", Command.ZZ1UNIT);
            _commandMap.Add("#1d3units", Command.ZZ1D3UNITS);
            _commandMap.Add("#2d3units", Command.ZZ2D3UNITS);
            _commandMap.Add("#3d3units", Command.ZZ3D3UNITS);
            _commandMap.Add("#4d3units", Command.ZZ4D3UNITS);
            _commandMap.Add("#1d6units", Command.ZZ1D6UNITS);
            _commandMap.Add("#2d6units", Command.ZZ2D6UNITS);
            _commandMap.Add("#3d6units", Command.ZZ3D6UNITS);
            _commandMap.Add("#4d6units", Command.ZZ4D6UNITS);
            _commandMap.Add("#5d6units", Command.ZZ5D6UNITS);
            _commandMap.Add("#6d6units", Command.ZZ6D6UNITS);
            _commandMap.Add("#7d6units", Command.ZZ7D6UNITS);
            _commandMap.Add("#8d6units", Command.ZZ8D6UNITS);
            _commandMap.Add("#9d6units", Command.ZZ9D6UNITS);
            _commandMap.Add("#10d6units", Command.ZZ10D6UNITS);
            _commandMap.Add("#11d6units", Command.ZZ11D6UNITS);
            _commandMap.Add("#12d6units", Command.ZZ12D6UNITS);
            _commandMap.Add("#13d6units", Command.ZZ13D6UNITS);
            _commandMap.Add("#14d6units", Command.ZZ14D6UNITS);
            _commandMap.Add("#15d6units", Command.ZZ15D6UNITS);
            _commandMap.Add("#16d6units", Command.ZZ16D6UNITS);
            _commandMap.Add("#strikeunits", Command.STRIKEUNITS);
            _commandMap.Add("#killmon", Command.KILLMON);
            _commandMap.Add("#kill2d6mon", Command.KILL2D6MON);
            _commandMap.Add("#killcom", Command.KILLCOM);
            _commandMap.Add("#researchaff", Command.RESEARCHAFF);
            _commandMap.Add("#gainaff", Command.GAINAFF);
            _commandMap.Add("#gainmark", Command.GAINMARK);
            _commandMap.Add("#banished", Command.BANISHED);
            _commandMap.Add("#addequip", Command.ADDEQUIP);
            _commandMap.Add("#transform", Command.TRANSFORM);
            _commandMap.Add("#fireboost", Command.FIREBOOST);
            _commandMap.Add("#airboost", Command.AIRBOOST);
            _commandMap.Add("#waterboost", Command.WATERBOOST);
            _commandMap.Add("#earthboost", Command.EARTHBOOST);
            _commandMap.Add("#astralboost", Command.ASTRALBOOST);
            _commandMap.Add("#deathboost", Command.DEATHBOOST);
            _commandMap.Add("#natureboost", Command.NATUREBOOST);
            _commandMap.Add("#bloodboost", Command.BLOODBOOST);
            _commandMap.Add("#holyboost", Command.HOLYBOOST);
            _commandMap.Add("#pathboost", Command.PATHBOOST);
            _commandMap.Add("#worldincscale", Command.WORLDINCSCALE);
            _commandMap.Add("#worldincscale2", Command.WORLDINCSCALE2);
            _commandMap.Add("#worldincscale3", Command.WORLDINCSCALE3);
            _commandMap.Add("#worlddecscale", Command.WORLDDECSCALE);
            _commandMap.Add("#worlddecscale2", Command.WORLDDECSCALE2);
            _commandMap.Add("#worlddecscale3", Command.WORLDDECSCALE3);
            _commandMap.Add("#worldunrest", Command.WORLDUNREST);
            _commandMap.Add("#worldincdom", Command.WORLDINCDOM);
            _commandMap.Add("#worldritrebate", Command.WORLDRITREBATE);
            _commandMap.Add("#worlddarkness", Command.WORLDDARKNESS);
            _commandMap.Add("#worldcurse", Command.WORLDCURSE);
            _commandMap.Add("#worlddisease", Command.WORLDDISEASE);
            _commandMap.Add("#worldmark", Command.WORLDMARK);
            _commandMap.Add("#worldheal", Command.WORLDHEAL);
            _commandMap.Add("#worldage", Command.WORLDAGE);
            _commandMap.Add("#linger", Command.LINGER);
            _commandMap.Add("#flagland", Command.FLAGLAND);
            _commandMap.Add("#delay", Command.DELAY);
            _commandMap.Add("#delay25", Command.DELAY25);
            _commandMap.Add("#delay50", Command.DELAY50);
            _commandMap.Add("#delayskip", Command.DELAYSKIP);
            _commandMap.Add("#order", Command.ORDER);
            _commandMap.Add("#code", Command.CODE);
            _commandMap.Add("#code2", Command.CODE2);
            _commandMap.Add("#resetcode", Command.RESETCODE);
            _commandMap.Add("#purgecalendar", Command.PURGECALENDAR);
            _commandMap.Add("#purgedelayed", Command.PURGEDELAYED);
            _commandMap.Add("#id", Command.ID);
            _commandMap.Add("#codedelay", Command.CODEDELAY);
            _commandMap.Add("#codedelay2", Command.CODEDELAY2);
            _commandMap.Add("#resetcodedelay", Command.RESETCODEDELAY);
            _commandMap.Add("#resetcodedelay2", Command.RESETCODEDELAY2);
            _commandMap.Add("#arena", Command.ARENA);
            _commandMap.Add("#resolvearena1", Command.RESOLVEARENA1);
            _commandMap.Add("#resolvearena2", Command.RESOLVEARENA2);
            _commandMap.Add("#extramsg", Command.EXTRAMSG);
            _commandMap.Add("#req_targnoaff", Command.REQ_TARGNOAFF);
            _commandMap.Add("#req_mincorpses", Command.REQ_MINCORPSES);
            _commandMap.Add("#req_maxcorpses", Command.REQ_MAXCORPSES);
            _commandMap.Add("#req_thronesite", Command.REQ_THRONESITE);
            _commandMap.Add("#setpoptype", Command.SETPOPTYPE);
            _commandMap.Add("#req_targnoorder", Command.REQ_TARGNOORDER);
            _commandMap.Add("#req_targminmorale", Command.REQ_TARGMINMORALE);
            _commandMap.Add("#req_targmaxmorale", Command.REQ_TARGMAXMORALE);
            _commandMap.Add("#req_targnotowner", Command.REQ_TARGNOTOWNER);
            _commandMap.Add("#req_targnotally", Command.REQ_TARGNOTALLY);
            _commandMap.Add("#req_targally", Command.REQ_TARGALLY);
            _commandMap.Add("#req_godisnotmnr", Command.REQ_GODISNOTMNR);
            _commandMap.Add("#req_worlditem", Command.REQ_WORLDITEM);
            _commandMap.Add("#req_noworlditem", Command.REQ_NOWORLDITEM);
            _commandMap.Add("#setxp", Command.SETXP);
            // Dominions 6 additions:
            _commandMap.Add("#acidres", Command.ACIDRES);
            _commandMap.Add("#decayres", Command.DECAYRES);
            _commandMap.Add("#heavyitem", Command.HEAVYITEM);
            _commandMap.Add("#nofemale", Command.NOFEMALE);
            _commandMap.Add("#onlyfemale", Command.ONLYFEMALE);
            _commandMap.Add("#danceweapon", Command.DANCEWEAPON);
            _commandMap.Add("#dancenratt", Command.DANCENRATT);
            _commandMap.Add("#dancespr", Command.DANCESPR);
            _commandMap.Add("#dancenof", Command.DANCENOF);
            _commandMap.Add("#dancesize", Command.DANCESIZE);
            _commandMap.Add("#bestowtomount", Command.BESTOWTOMOUNT);
            _commandMap.Add("#mobilearcher", Command.MOBILEARCHER);
            _commandMap.Add("#statstorm", Command.STATSTORM);
            _commandMap.Add("#statbreak", Command.STATBREAK);
            _commandMap.Add("#plaguedoctor", Command.PLAGUEDOCTOR);
            _commandMap.Add("#corruptor", Command.CORRUPTOR);
            _commandMap.Add("#icenatprot", Command.ICENATPROT);
            _commandMap.Add("#maxdeadhp", Command.MAXDEADHP);
            _commandMap.Add("#xpgain", Command.XPGAIN);
            _commandMap.Add("#clumsy", Command.CLUMSY);
            _commandMap.Add("#truesight", Command.TRUESIGHT);
            _commandMap.Add("#twistfate", Command.TWISTFATE);
            _commandMap.Add("#powerofdeath", Command.POWEROFDEATH);
            _commandMap.Add("#fearofflood", Command.FEAROFFLOOD);
            _commandMap.Add("#mindcollar", Command.MINDCOLLAR);
            _commandMap.Add("#falsesupply", Command.FALSESUPPLY);
            _commandMap.Add("#elementgems", Command.ELEMENTGEMS);
            _commandMap.Add("#sorcerygems", Command.SORCERYGEMS);
            _commandMap.Add("#glamourmanip", Command.GLAMOURMANIP);
            _commandMap.Add("#glamourrange", Command.GLAMOURRANGE);
            _commandMap.Add("#holyrange", Command.HOLYRANGE);
            _commandMap.Add("#tmpglamourgems", Command.TMPGLAMOURGEMS);
            _commandMap.Add("#fireelementals", Command.FIREELEMENTALS);
            _commandMap.Add("#airelementals", Command.AIRELEMENTALS);
            _commandMap.Add("#earthelementals", Command.EARTHELEMENTALS);
            _commandMap.Add("#waterelementals", Command.WATERELEMENTALS);
            _commandMap.Add("#undisleader", Command.UNDISLEADER);
            _commandMap.Add("#thirdstr", Command.THIRDSTR);
            _commandMap.Add("#spiritformimmune", Command.SPIRITFORMIMMUNE);
            _commandMap.Add("#illusionsimmune", Command.ILLUSIONSIMMUNE);
            _commandMap.Add("#false", Command.FALSE);
            _commandMap.Add("#speedmult", Command.SPEEDMULT);
            _commandMap.Add("#notmounted", Command.NOTMOUNTED);
            _commandMap.Add("#notdismounted", Command.NOTDISMOUNTED);
            _commandMap.Add("#holyifhit", Command.HOLYIFHIT);
            _commandMap.Add("#killmagicifhit", Command.KILLMAGICIFHIT);
            _commandMap.Add("#killdemonifhit", Command.KILLDEMONIFHIT);
            _commandMap.Add("#holystunifhit", Command.HOLYSTUNIFHIT);
            _commandMap.Add("#petrifyifhit", Command.PETRIFYIFHIT);
            _commandMap.Add("#fireifhit", Command.FIREIFHIT);
            _commandMap.Add("#coldifhit", Command.COLDIFHIT);
            _commandMap.Add("#shockifhit", Command.SHOCKIFHIT);
            _commandMap.Add("#poisonifdmg", Command.POISONIFDMG);
            _commandMap.Add("#aftercloud", Command.AFTERCLOUD);
            _commandMap.Add("#aftercloudarea", Command.AFTERCLOUDAREA);
            _commandMap.Add("#godsite", Command.GODSITE);
            _commandMap.Add("#moreorder", Command.MOREORDER);
            _commandMap.Add("#moreprod", Command.MOREPROD);
            _commandMap.Add("#moreheat", Command.MOREHEAT);
            _commandMap.Add("#moregrowth", Command.MOREGROWTH);
            _commandMap.Add("#moreluck", Command.MORELUCK);
            _commandMap.Add("#moremagic", Command.MOREMAGIC);
            _commandMap.Add("#enchrebate75", Command.ENCHREBATE75);
            _commandMap.Add("#enchrebate100", Command.ENCHREBATE100);
            _commandMap.Add("#heatrecscale", Command.HEATRECSCALE);
            _commandMap.Add("#coldrecscale", Command.COLDRECSCALE);
            _commandMap.Add("#growthrecscale", Command.GROWTHRECSCALE);
            _commandMap.Add("#deathrecscale", Command.DEATHRECSCALE);
            _commandMap.Add("#orderrecscale", Command.ORDERRECSCALE);
            _commandMap.Add("#chaosrecscale", Command.CHAOSRECSCALE);
            _commandMap.Add("#noweapon", Command.NOWEAPON);
            _commandMap.Add("#holycost", Command.HOLYCOST);
            _commandMap.Add("#spiritform", Command.SPIRITFORM);
            _commandMap.Add("#nospiritform", Command.NOSPIRITFORM);
            _commandMap.Add("#divinebeing", Command.DIVINEBEING);
            _commandMap.Add("#assencloc", Command.ASSENCLOC);
            _commandMap.Add("#autocorpsehealer", Command.AUTOCORPSEHEALER);
            _commandMap.Add("#extralives", Command.EXTRALIVES);
            _commandMap.Add("#reconst", Command.RECONST);
            _commandMap.Add("#falseregen", Command.FALSEREGEN);
            _commandMap.Add("#glamour", Command.GLAMOUR);
            _commandMap.Add("#glamourattuned", Command.GLAMOURATTUNED);
            _commandMap.Add("#dread", Command.DREAD);
            _commandMap.Add("#nightmareaura", Command.NIGHTMAREAURA);
            _commandMap.Add("#autoberserk", Command.AUTOBERSERK);
            _commandMap.Add("#deathpoison", Command.DEATHPOISON);
            _commandMap.Add("#deathshock", Command.DEATHSHOCK);
            _commandMap.Add("#deathslime", Command.DEATHSLIME);
            _commandMap.Add("#deathgrab", Command.DEATHGRAB);
            _commandMap.Add("#labxpshape", Command.LABXPSHAPE);
            _commandMap.Add("#xpshapemon", Command.XPSHAPEMON);
            _commandMap.Add("#xpshapeloss", Command.XPSHAPELOSS);
            _commandMap.Add("#lich", Command.LICH);
            _commandMap.Add("#battleshape", Command.BATTLESHAPE);
            _commandMap.Add("#worldshape", Command.WORLDSHAPE);
            _commandMap.Add("#animated", Command.ANIMATED);
            _commandMap.Add("#mountmnr", Command.MOUNTMNR);
            _commandMap.Add("#unmountedspr1", Command.UNMOUNTEDSPR1);
            _commandMap.Add("#unmountedspr2", Command.UNMOUNTEDSPR2);
            _commandMap.Add("#nofmounts", Command.NOFMOUNTS);
            _commandMap.Add("#nofriders", Command.NOFRIDERS);
            _commandMap.Add("#coridermnr", Command.CORIDERMNR);
            _commandMap.Add("#xspr1", Command.XSPR1);
            _commandMap.Add("#xspr2", Command.XSPR2);
            _commandMap.Add("#regainmount", Command.REGAINMOUNT);
            _commandMap.Add("#noremount", Command.NOREMOUNT);
            _commandMap.Add("#skilledrider", Command.SKILLEDRIDER);
            _commandMap.Add("#mountiscom", Command.MOUNTISCOM);
            _commandMap.Add("#nobarding", Command.NOBARDING);
            _commandMap.Add("#nothrowoff", Command.NOTHROWOFF);
            _commandMap.Add("#nofalldmg", Command.NOFALLDMG);
            _commandMap.Add("#bravemount", Command.BRAVEMOUNT);
            _commandMap.Add("#smartmount", Command.SMARTMOUNT);
            _commandMap.Add("#look", Command.LOOK);
            _commandMap.Add("#defcom", Command.DEFCOM);
            _commandMap.Add("#defunit", Command.DEFUNIT);
            _commandMap.Add("#defmult", Command.DEFMULT);
            _commandMap.Add("#popgrowth", Command.POPGROWTH);
            _commandMap.Add("#viewallprov", Command.VIEWALLPROV);
            _commandMap.Add("#viewallbat", Command.VIEWALLBAT);
            _commandMap.Add("#aiglamournation", Command.AIGLAMOURNATION);
            _commandMap.Add("#guardcom", Command.GUARDCOM);
            _commandMap.Add("#guardunit", Command.GUARDUNIT);
            _commandMap.Add("#guardmult", Command.GUARDMULT);
            _commandMap.Add("#seatrace", Command.SEATRACE);
            _commandMap.Add("#disbless", Command.DISBLESS);
            _commandMap.Add("#hidedom", Command.HIDEDOM);
            _commandMap.Add("#templeholypoints", Command.TEMPLEHOLYPOINTS);
            _commandMap.Add("#forestfortrec", Command.FORESTFORTREC);
            _commandMap.Add("#forestfortcom", Command.FORESTFORTCOM);
            _commandMap.Add("#mountainfortrec", Command.MOUNTAINFORTREC);
            _commandMap.Add("#mountainfortcom", Command.MOUNTAINFORTCOM);
            _commandMap.Add("#swampfortrec", Command.SWAMPFORTREC);
            _commandMap.Add("#swampfortcom", Command.SWAMPFORTCOM);
            _commandMap.Add("#wastefortrec", Command.WASTEFORTREC);
            _commandMap.Add("#wastefortcom", Command.WASTEFORTCOM);
            _commandMap.Add("#farmrec", Command.FARMREC);
            _commandMap.Add("#farmfortrec", Command.FARMFORTREC);
            _commandMap.Add("#farmcom", Command.FARMCOM);
            _commandMap.Add("#farmfortcom", Command.FARMFORTCOM);
            _commandMap.Add("#cavefortrec", Command.CAVEFORTREC);
            _commandMap.Add("#cavefortcom", Command.CAVEFORTCOM);
            _commandMap.Add("#driprec", Command.DRIPREC);
            _commandMap.Add("#dripfortrec", Command.DRIPFORTREC);
            _commandMap.Add("#dripcom", Command.DRIPCOM);
            _commandMap.Add("#dripfortcom", Command.DRIPFORTCOM);
            _commandMap.Add("#coastfortrec", Command.COASTFORTREC);
            _commandMap.Add("#coastfortcom", Command.COASTFORTCOM);
            _commandMap.Add("#searec", Command.SEAREC);
            _commandMap.Add("#seafortrec", Command.SEAFORTREC);
            _commandMap.Add("#seacom", Command.SEACOM);
            _commandMap.Add("#seafortcom", Command.SEAFORTCOM);
            _commandMap.Add("#deeprec", Command.DEEPREC);
            _commandMap.Add("#deepfortrec", Command.DEEPFORTREC);
            _commandMap.Add("#deepcom", Command.DEEPCOM);
            _commandMap.Add("#deepfortcom", Command.DEEPFORTCOM);
            _commandMap.Add("#kelprec", Command.KELPREC);
            _commandMap.Add("#kelpfortrec", Command.KELPFORTREC);
            _commandMap.Add("#kelpcom", Command.KELPCOM);
            _commandMap.Add("#kelpfortcom", Command.KELPFORTCOM);
            _commandMap.Add("#onlysitedst", Command.ONLYSITEDST);
            _commandMap.Add("#dispimmune", Command.DISPIMMUNE);
            _commandMap.Add("#napbreakrit", Command.NAPBREAKRIT);
            _commandMap.Add("#sumhealaffs", Command.SUMHEALAFFS);
            _commandMap.Add("#localglobal", Command.LOCALGLOBAL);
            _commandMap.Add("#worldvisible", Command.WORLDVISIBLE);
            _commandMap.Add("#globallook", Command.GLOBALLOOK);
            _commandMap.Add("#portent", Command.PORTENT);
            _commandMap.Add("#cure", Command.CURE);
            _commandMap.Add("#reqnospellsinger", Command.REQNOSPELLSINGER);
            _commandMap.Add("#reqnotaskmaster", Command.REQNOTASKMASTER);
            _commandMap.Add("#reqnoseduce", Command.REQNOSEDUCE);
            _commandMap.Add("#aiassmod", Command.AIASSMOD);
            _commandMap.Add("#req_turnrare", Command.REQ_TURNRARE);
            _commandMap.Add("#req_month", Command.REQ_MONTH);
            _commandMap.Add("#req_ai", Command.REQ_AI);
            _commandMap.Add("#req_nearbycapital", Command.REQ_NEARBYCAPITAL);
            _commandMap.Add("#req_notpoptype", Command.REQ_NOTPOPTYPE);
            _commandMap.Add("#req_voidok", Command.REQ_VOIDOK);
            _commandMap.Add("#req_fortid", Command.REQ_FORTID);
            _commandMap.Add("#req_plane", Command.REQ_PLANE);
            _commandMap.Add("#req_kelp", Command.REQ_KELP);
            _commandMap.Add("#req_gorge", Command.REQ_GORGE);
            _commandMap.Add("#req_deep", Command.REQ_DEEP);
            _commandMap.Add("#req_forestcave", Command.REQ_FORESTCAVE);
            _commandMap.Add("#req_drip", Command.REQ_DRIP);
            _commandMap.Add("#req_crystal", Command.REQ_CRYSTAL);
            _commandMap.Add("#req_nativesoil", Command.REQ_NATIVESOIL);
            _commandMap.Add("#req_void", Command.REQ_VOID);
            _commandMap.Add("#req_godawake", Command.REQ_GODAWAKE);
            _commandMap.Add("#req_pretismnr", Command.REQ_PRETISMNR);
            _commandMap.Add("#req_pretawake", Command.REQ_PRETAWAKE);
            _commandMap.Add("#req_pathglamour", Command.REQ_PATHGLAMOUR);
            _commandMap.Add("#req_nopathglamour", Command.REQ_NOPATHGLAMOUR);
            _commandMap.Add("#req_targsight", Command.REQ_TARGSIGHT);
            _commandMap.Add("#req_targmanygems", Command.REQ_TARGMANYGEMS);
            _commandMap.Add("#req_targhorrormark", Command.REQ_TARGHORRORMARK);
            _commandMap.Add("#req_targinsane", Command.REQ_TARGINSANE);
            _commandMap.Add("#req_targseductions", Command.REQ_TARGSEDUCTIONS);
            _commandMap.Add("#req_targminkills", Command.REQ_TARGMINKILLS);
            _commandMap.Add("#req_targmaxkills", Command.REQ_TARGMAXKILLS);
            _commandMap.Add("#req_notcode", Command.REQ_NOTCODE);
            _commandMap.Add("#req_enchtarget", Command.REQ_ENCHTARGET);
            _commandMap.Add("#req_enchnearby", Command.REQ_ENCHNEARBY);
            _commandMap.Add("#req_minglobals", Command.REQ_MINGLOBALS);
            _commandMap.Add("#req_maxglobals", Command.REQ_MAXGLOBALS);
            _commandMap.Add("#req_varpos", Command.REQ_VARPOS);
            _commandMap.Add("#req_varneg", Command.REQ_VARNEG);
            _commandMap.Add("#req_varzero", Command.REQ_VARZERO);
            _commandMap.Add("#req_varone", Command.REQ_VARONE);
            _commandMap.Add("#assownerench", Command.ASSOWNERENCH);
            _commandMap.Add("#assfollower1", Command.ASSFOLLOWER1);
            _commandMap.Add("#assfollower2", Command.ASSFOLLOWER2);
            _commandMap.Add("#assfollower3", Command.ASSFOLLOWER3);
            _commandMap.Add("#assfollower1d3", Command.ASSFOLLOWER1D3);
            _commandMap.Add("#healaff", Command.HEALAFF);
            _commandMap.Add("#forcetransform", Command.FORCETRANSFORM);
            _commandMap.Add("#remount", Command.REMOUNT);
            _commandMap.Add("#cleartarg", Command.CLEARTARG);
            _commandMap.Add("#killtarg", Command.KILLTARG);
            _commandMap.Add("#addseductions", Command.ADDSEDUCTIONS);
            _commandMap.Add("#addkills", Command.ADDKILLS);
            _commandMap.Add("#glamourboost", Command.GLAMOURBOOST);
            _commandMap.Add("#dispglobals", Command.DISPGLOBALS);
            _commandMap.Add("#clearvar", Command.CLEARVAR);
            _commandMap.Add("#incvar", Command.INCVAR);
            _commandMap.Add("#decvar", Command.DECVAR);
            _commandMap.Add("#inc10var", Command.INC10VAR);
            _commandMap.Add("#dec10var", Command.DEC10VAR);
            _commandMap.Add("#invvar", Command.INVVAR);
            _commandMap.Add("#togglevar", Command.TOGGLEVAR);
            _commandMap.Add("#addascension", Command.ADDASCENSION);
            _commandMap.Add("#minascension", Command.MINASCENSION);
            _commandMap.Add("#req_nearbythrone", Command.REQ_NEARBYTHRONE);
            _commandMap.Add("#header", Command.HEADER);
            _commandMap.Add("#forcegold", Command.FORCEGOLD);
            _commandMap.Add("#forceexactgold", Command.FORCEEXACTGOLD);
            _commandMap.Add("#force1d3vis", Command.FORCE1D3VIS);
            _commandMap.Add("#force1d6vis", Command.FORCE1D6VIS);
            _commandMap.Add("#force2d4vis", Command.FORCE2D4VIS);
            _commandMap.Add("#force2d6vis", Command.FORCE2D6VIS);
            _commandMap.Add("#force3d6vis", Command.FORCE3D6VIS);
            _commandMap.Add("#force4d6vis", Command.FORCE4D6VIS);
            _commandMap.Add("#gemlosssmall", Command.GEMLOSSSMALL);
            _commandMap.Add("#gemlosslarge", Command.GEMLOSSLARGE);
            _commandMap.Add("#addgeo", Command.ADDGEO);
            _commandMap.Add("#remgeo", Command.REMGEO);
            _commandMap.Add("#startresearch", Command.STARTRESEARCH);
            _commandMap.Add("#arenagold", Command.ARENAGOLD);
            _commandMap.Add("#arenagems", Command.ARENAGEMS);
            _commandMap.Add("#gemlongevity", Command.GEMLONGEVITY);
            //NON EDITABLE
            _commandMap.Add("#corrupt", Command.CORRUPT);
            _commandMap.Add("#reincarnation", Command.REINCARNATION);
            _commandMap.Add("#petrify", Command.PETRIFY);
            _commandMap.Add("#realm2", Command.REALM2);
            _commandMap.Add("#realm3", Command.REALM3);
            _commandMap.Add("#turmoilsummon", Command.TURMOILSUMMON);
            _commandMap.Add("#coldsummon", Command.COLDSUMMON);
            _commandMap.Add("#preanimator", Command.PREANIMATOR);
            _commandMap.Add("#dreanimator", Command.DREANIMATOR);
            _commandMap.Add("#horror", Command.HORROR);
            _commandMap.Add("#sizewhensailing", Command.SIZEWHENSAILING);
            _commandMap.Add("#mummification", Command.MUMMIFICATION);
            _commandMap.Add("#sendlessorhorrormult", Command.SENDLESSORHORRORMULT);
            _commandMap.Add("#curseattacker", Command.CURSEATTACKER);
            _commandMap.Add("#disbelieve", Command.DISBELIEVE);
            _commandMap.Add("#landreinvigoration", Command.LANDREINVIGORATION);
            _commandMap.Add("#astralfetters", Command.ASTRALFETTERS);
            _commandMap.Add("#foreignmagicboost", Command.FOREIGNMAGICBOOST);
            _commandMap.Add("#stunimmunity", Command.STUNIMMUNITY);
            _commandMap.Add("#adventurers", Command.ADVENTURERS);
            _commandMap.Add("#horrormarked", Command.HORRORMARKED);
            _commandMap.Add("#changetargetgenderforseductionandseductionimmune", Command.CHANGETARGETGENDERFORSEDUCTIONANDSEDUCTIONIMMUNE);
            _commandMap.Add("#corpseconstruct", Command.CORPSECONSTRUCT);
            _commandMap.Add("#isashah", Command.ISASHAH);
            _commandMap.Add("#isayazad", Command.ISAYAZAD);
            _commandMap.Add("#isadaeva", Command.ISADAEVA);
            _commandMap.Add("#clockworklord", Command.CLOCKWORKLORD);
            _commandMap.Add("#stupid", Command.STUPID);
            _commandMap.Add("#heathensummon", Command.HEATHENSUMMON);
            _commandMap.Add("#unseen", Command.UNSEEN);
            _commandMap.Add("#illusionary", Command.ILLUSIONARY);
            _commandMap.Add("#wolf", Command.WOLF);
            _commandMap.Add("#aboleth", Command.ABOLETH);
            _commandMap.Add("#defiler", Command.DEFILER);
            _commandMap.Add("#mountedberserk", Command.MOUNTEDBERSERK);
            _commandMap.Add("#landenc", Command.LANDENC);
            _commandMap.Add("#pathboostuw", Command.PATHBOOSTUW);
            _commandMap.Add("#pathboostland", Command.PATHBOOSTLAND);
            _commandMap.Add("#percentpathreduction", Command.PERCENTPATHREDUCTION);
            _commandMap.Add("#labpromotion", Command.LABPROMOTION);
            _commandMap.Add("#undying", Command.UNDYING);
            _commandMap.Add("#moralebonus", Command.MORALEBONUS);
            _commandMap.Add("#uncurableaffliction", Command.UNCURABLEAFFLICTION);
            _commandMap.Add("#autoblessed", Command.AUTOBLESSED);
            _commandMap.Add("#wintersummon1d3", Command.WINTERSUMMON1D3);
            _commandMap.Add("#stygianguide", Command.STYGIANGUIDE);
            // Dom6 additions for weapon
            _commandMap.Add("#defroll", Command.DEFROLL);
            _commandMap.Add("#dmginspector", Command.DMGINSPECTOR);
            _commandMap.Add("#morroll", Command.MORROLL);
            // Dom6 additions for armor
            _commandMap.Add("#protparts", Command.PROTPARTS);
            _commandMap.Add("#protinspector", Command.PROTINSPECTOR);
            // Dom6 additions for monster
            _commandMap.Add("#mountedinspector", Command.MOUNTEDINSPECTOR);
            _commandMap.Add("#growthpower", Command.GROWTHPOWER);
            _commandMap.Add("#iceprotinspector", Command.ICEPROTINSPECTOR);
            _commandMap.Add("#praise", Command.PRAISE);
            _commandMap.Add("#mountedspr1", Command.MOUNTEDSPR1);
            _commandMap.Add("#mountedspr2", Command.MOUNTEDSPR2);
            _commandMap.Add("#enchantedblood", Command.ENCHANTEDBLOOD);
            _commandMap.Add("#regainmounts", Command.REGAINMOUNTS);
            _commandMap.Add("#regen", Command.REGEN);
            _commandMap.Add("#colres", Command.COLRES);
            _commandMap.Add("#amphibious", Command.AMPHIBIOUS);
            // Dom6 additions for site
            _commandMap.Add("#copysite", Command.COPYSITE);
            // Dom6 additions for nation
            _commandMap.Add("#blessbonus", Command.BLESSBONUS);
            _commandMap.Add("#plainfortrec", Command.PLAINFORTREC);
            _commandMap.Add("#plainfortcom", Command.PLAINFORTCOM);
            _commandMap.Add("#foreignfortrec", Command.FOREIGNFORTREC);
            _commandMap.Add("#foreignfortcom", Command.FOREIGNFORTCOM);
            _commandMap.Add("#uwguardcom", Command.UWGUARDCOM);
            _commandMap.Add("#uwguardunit", Command.UWGUARDUNIT);
            _commandMap.Add("#uwguardmult", Command.UWGUARDMULT);
            _commandMap.Add("#fortheatscaleres", Command.FORTHEATSCALERES);
            _commandMap.Add("#fortcoldscaleres", Command.FORTCOLDSCALERES);
            _commandMap.Add("#homeheatscaleres", Command.HOMEHEATSCALERES);
            _commandMap.Add("#plainrec", Command.PLAINREC);
            _commandMap.Add("#caveinc", Command.CAVEINC);
            _commandMap.Add("#caveres", Command.CAVERES);
            _commandMap.Add("#caverecpt", Command.CAVERECPT);
            // Dom6 additions for spell
            _commandMap.Add("#aoeinspector", Command.AOEINSPECTOR);
            _commandMap.Add("#fatiguecostinspector", Command.FATIGUECOSTINSPECTOR);
            // Dom6 additions for item
            _commandMap.Add("#taint", Command.TAINT);
            // Dom6 additions for event
            _commandMap.Add("#req_targrealmnr", Command.REQ_TARGREALMNR);
            _commandMap.Add("#selectevent", Command.SELECTEVENT);

            // Read by the Dominions 6.37 parser (tools/dom6exe catalog)
            _commandMap.Add("#arena1", Command.ARENA1);
            _commandMap.Add("#arena2", Command.ARENA2);
            _commandMap.Add("#battlesum1d2", Command.BATTLESUM1D2);
            _commandMap.Add("#battlesum1d3", Command.BATTLESUM1D3);
            _commandMap.Add("#battlesumwarm", Command.BATTLESUMWARM);
            _commandMap.Add("#beauty", Command.BEAUTY);
            _commandMap.Add("#bugshape", Command.BUGSHAPE);
            _commandMap.Add("#bugswarmshape", Command.BUGSWARMSHAPE);
            _commandMap.Add("#bugswarmuwshape", Command.BUGSWARMUWSHAPE);
            _commandMap.Add("#buguwshape", Command.BUGUWSHAPE);
            _commandMap.Add("#corpsehealer", Command.CORPSEHEALER);
            _commandMap.Add("#faysummon", Command.FAYSUMMON);
            _commandMap.Add("#foreignguardcom", Command.FOREIGNGUARDCOM);
            _commandMap.Add("#foreignguardmult", Command.FOREIGNGUARDMULT);
            _commandMap.Add("#foreignguardunit", Command.FOREIGNGUARDUNIT);
            _commandMap.Add("#foreignwallcom", Command.FOREIGNWALLCOM);
            _commandMap.Add("#foreignwallmult", Command.FOREIGNWALLMULT);
            _commandMap.Add("#foreignwallunit", Command.FOREIGNWALLUNIT);
            _commandMap.Add("#forestsurv", Command.FORESTSURV);
            _commandMap.Add("#gainaffmount", Command.GAINAFFMOUNT);
            _commandMap.Add("#glamourblessbonus", Command.GLAMOURBLESSBONUS);
            _commandMap.Add("#grandcom", Command.GRANDCOM);
            _commandMap.Add("#healaffmount", Command.HEALAFFMOUNT);
            _commandMap.Add("#homecoldscaleres", Command.HOMECOLDSCALERES);
            _commandMap.Add("#magiconly", Command.MAGICONLY);
            _commandMap.Add("#makecrater", Command.MAKECRATER);
            _commandMap.Add("#maybeaddsite", Command.MAYBEADDSITE);
            _commandMap.Add("#maybehiddensite", Command.MAYBEHIDDENSITE);
            _commandMap.Add("#mountsurv", Command.MOUNTSURV);
            _commandMap.Add("#mrhalf", Command.MRHALF);
            _commandMap.Add("#newnbor", Command.NEWNBOR);
            _commandMap.Add("#notindoors", Command.NOTINDOORS);
            _commandMap.Add("#onlysleepers", Command.ONLYSLEEPERS);
            _commandMap.Add("#plaincom", Command.PLAINCOM);
            _commandMap.Add("#remnbor", Command.REMNBOR);
            _commandMap.Add("#req_caveforest", Command.REQ_CAVEFOREST);
            _commandMap.Add("#req_deepsea", Command.REQ_DEEPSEA);
            _commandMap.Add("#req_minresearch", Command.REQ_MINRESEARCH);
            _commandMap.Add("#req_mnrbs", Command.REQ_MNRBS);
            _commandMap.Add("#req_monsterbs", Command.REQ_MONSTERBS);
            _commandMap.Add("#req_norealmnr", Command.REQ_NOREALMNR);
            _commandMap.Add("#req_path", Command.REQ_PATH);
            _commandMap.Add("#req_pathgems", Command.REQ_PATHGEMS);
            _commandMap.Add("#req_provnbr", Command.REQ_PROVNBR);
            _commandMap.Add("#req_realmnr", Command.REQ_REALMNR);
            _commandMap.Add("#req_school", Command.REQ_SCHOOL);
            _commandMap.Add("#req_targnorealmnr", Command.REQ_TARGNOREALMNR);
            _commandMap.Add("#sabbathmaster", Command.SABBATHMASTER);
            _commandMap.Add("#sabbathslave", Command.SABBATHSLAVE);
            _commandMap.Add("#sizecost", Command.SIZECOST);
            _commandMap.Add("#spec2", Command.SPEC2);
            _commandMap.Add("#startunitnbrs3", Command.STARTUNITNBRS3);
            _commandMap.Add("#startunittype3", Command.STARTUNITTYPE3);
            _commandMap.Add("#statsiege", Command.STATSIEGE);
            _commandMap.Add("#swampsurv", Command.SWAMPSURV);
            _commandMap.Add("#tolerateund", Command.TOLERATEUND);
            _commandMap.Add("#twiceborncost", Command.TWICEBORNCOST);
            _commandMap.Add("#var0units", Command.VAR0UNITS);
            _commandMap.Add("#wastesurv", Command.WASTESURV);

            // Inspector-era list of commands the game doesn't read; GameData/GameCommandCatalog
            // (from the game's own parser) is authoritative. #unseen, #plaguedoctor and #mindcollar
            // were here but the game reads them.
            _nonEditable.Add(Command.CORRUPT);
            _nonEditable.Add(Command.REINCARNATION);
            _nonEditable.Add(Command.PETRIFY);
            _nonEditable.Add(Command.REALM2);
            _nonEditable.Add(Command.REALM3);
            _nonEditable.Add(Command.TURMOILSUMMON);
            _nonEditable.Add(Command.COLDSUMMON);
            _nonEditable.Add(Command.PREANIMATOR);
            _nonEditable.Add(Command.DREANIMATOR);
            _nonEditable.Add(Command.HORROR);
            _nonEditable.Add(Command.SIZEWHENSAILING);
            _nonEditable.Add(Command.MUMMIFICATION);
            _nonEditable.Add(Command.SENDLESSORHORRORMULT);
            _nonEditable.Add(Command.CURSEATTACKER);
            _nonEditable.Add(Command.DISBELIEVE);
            _nonEditable.Add(Command.LANDREINVIGORATION);
            _nonEditable.Add(Command.ASTRALFETTERS);
            _nonEditable.Add(Command.FOREIGNMAGICBOOST);
            _nonEditable.Add(Command.STUNIMMUNITY);
            _nonEditable.Add(Command.ADVENTURERS);
            _nonEditable.Add(Command.HORRORMARKED);
            _nonEditable.Add(Command.CHANGETARGETGENDERFORSEDUCTIONANDSEDUCTIONIMMUNE);
            _nonEditable.Add(Command.CORPSECONSTRUCT);
            _nonEditable.Add(Command.ISASHAH);
            _nonEditable.Add(Command.ISAYAZAD);
            _nonEditable.Add(Command.ISADAEVA);
            _nonEditable.Add(Command.CLOCKWORKLORD);
            _nonEditable.Add(Command.STUPID);
            _nonEditable.Add(Command.HEATHENSUMMON);
            _nonEditable.Add(Command.ILLUSIONARY);
            _nonEditable.Add(Command.WOLF);
            _nonEditable.Add(Command.ABOLETH);
            _nonEditable.Add(Command.DEFILER);
            _nonEditable.Add(Command.MOUNTEDBERSERK);
            _nonEditable.Add(Command.LANDENC);
            _nonEditable.Add(Command.PATHBOOSTUW);
            _nonEditable.Add(Command.PATHBOOSTLAND);
            _nonEditable.Add(Command.PERCENTPATHREDUCTION);
            _nonEditable.Add(Command.LABPROMOTION);
            _nonEditable.Add(Command.UNDYING);
            _nonEditable.Add(Command.MORALEBONUS);
            _nonEditable.Add(Command.UNCURABLEAFFLICTION);
            _nonEditable.Add(Command.AUTOBLESSED);
            _nonEditable.Add(Command.WINTERSUMMON1D3);
            _nonEditable.Add(Command.STYGIANGUIDE);

            // Bless entity commands
            _commandMap.Add("#selectbless", Command.SELECTBLESS);
            _commandMap.Add("#path0", Command.PATH0);
            _commandMap.Add("#cost0", Command.COST0);
            _commandMap.Add("#path1", Command.PATH1);
            _commandMap.Add("#cost1", Command.COST1);
            _commandMap.Add("#clearscales", Command.CLEARSCALES);
            _commandMap.Add("#orderscale", Command.ORDERSCALE);
            _commandMap.Add("#prodscale", Command.PRODSCALE);
            _commandMap.Add("#heatscale", Command.HEATSCALE);
            _commandMap.Add("#growthscale", Command.GROWTHSCALE);
            _commandMap.Add("#luckscale", Command.LUCKSCALE);
            _commandMap.Add("#magicscale", Command.MAGICSCALE);
            _commandMap.Add("#chaosscale", Command.CHAOSSCALE);
            _commandMap.Add("#slothscale", Command.SLOTHSCALE);
            _commandMap.Add("#coldscale", Command.COLDSCALE);
            _commandMap.Add("#deathscale", Command.DEATHSCALE);
            _commandMap.Add("#misfortscale", Command.MISFORTSCALE);
            _commandMap.Add("#drainscale", Command.DRAINSCALE);
            _commandMap.Add("#clearfx", Command.CLEARFX);

            // Template entity commands (AI pretender design)
            _commandMap.Add("#newtemplate", Command.NEWTEMPLATE);
            _commandMap.Add("#form", Command.FORM);
            _commandMap.Add("#prison", Command.PRISON);
            _commandMap.Add("#domstr", Command.DOMSTR);
            _commandMap.Add("#scale", Command.SCALE);
            _commandMap.Add("#researchgoal", Command.RESEARCHGOAL);
            _commandMap.Add("#favrit", Command.FAVRIT);

            //Build an inverted map
            foreach (KeyValuePair<string, Command> kvp in _commandMap)
            {
                _stringMap.Add(kvp.Value, kvp.Key);
            }
        }

        public static bool TryGetCommand(string s, out Command c)
        {
            return _commandMap.TryGetValue(s, out c);
        }

        public static bool TryGetString(Command c, out string s)
        {
            return _stringMap.TryGetValue(c, out s);
        }

        public static bool TryGetTooltip(Command c, out string s)
        {
            return _toolTipMap.TryGetValue(c, out s);
        }

        public static bool IsNotEditable(Command c)
        {
            return _nonEditable.Contains(c);
        }

        public static bool IsSingular(Command c)
        {
            return _singularMap.Contains(c);
        }

        public static string Format(Command c, string val, bool needsQuotes = false)
        {
            if (TryGetString(c, out string v))
            {
                if (needsQuotes)
                {
                    return v + " \"" + val + "\"";
                }
                else
                {
                    return v + " " + val;
                }
            }
            else
            {
                return "";
            }
        }
    }

    public enum MagicPaths
    {
        FIRE = 0,
        AIR = 1,
        WATER = 2,
        EARTH = 3,
        ASTRAL = 4,
        DEATH = 5,
        NATURE = 6,
        GLAMOUR = 7,
        BLOOD = 8,
        PRIEST = 9,
        RANDOM = 50,
        ELEMENTAL = 51,
        SORCERY = 52,
        ALL = 53
    }

    public struct MagicSkill
    {
        public MagicPaths Path;
        public int Level;
    }

    public struct CustomMagic
    {
        public List<MagicPaths> Path;
        public double Chance;
    }
}
