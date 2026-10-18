using Dom5Edit.Commands;
using Dom5Edit.Props;

namespace Dom5Edit.Entities
{
    public class Event : IDEntity
    {
        public static Dictionary<Command, Func<Property>> _propertyMap = new Dictionary<Command, Func<Property>>();

        static Event()
        {
            _propertyMap.Add(Command.RARITY, IntProperty.Create);
            _propertyMap.Add(Command.REQ_RARE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_UNIQUE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_STORY, IntProperty.Create);
            _propertyMap.Add(Command.REQ_INDEPOK, IntProperty.Create);
            _propertyMap.Add(Command.REQ_ERA, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOERA, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TURN, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXTURN, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PREGAME, IntProperty.Create);
            _propertyMap.Add(Command.REQ_SEASON, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOSEASON, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NATION, NationRef.Create);
            _propertyMap.Add(Command.REQ_NONATION, NationRef.Create);
            _propertyMap.Add(Command.REQ_FORNATION, NationRef.Create);
            _propertyMap.Add(Command.REQ_NOTNATION, NationRef.Create);
            _propertyMap.Add(Command.REQ_NOTFORNATION, NationRef.Create);
            _propertyMap.Add(Command.REQ_NOTFORALLY, NationRef.Create);
            _propertyMap.Add(Command.EXTRAMSG, NationRef.Create);
            _propertyMap.Add(Command.REQ_GEM, IntProperty.Create);
            _propertyMap.Add(Command.REQ_GOLD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_CAPITAL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_OWNCAPITAL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_POPTYPE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_POP0OK, CommandProperty.Create);
            _propertyMap.Add(Command.REQ_MAXPOP, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MINPOP, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MINDEF, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXDEF, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MINUNREST, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXUNREST, IntProperty.Create);
            _propertyMap.Add(Command.REQ_LAB, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TEMPLE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FORT, IntProperty.Create);
            _propertyMap.Add(Command.REQ_LAND, IntProperty.Create);
            _propertyMap.Add(Command.REQ_COAST, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MOUNTAIN, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FOREST, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FARM, IntProperty.Create);
            _propertyMap.Add(Command.REQ_SWAMP, IntProperty.Create);
            _propertyMap.Add(Command.REQ_WASTE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_CAVE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FRESHWATER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FREESITES, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOSITENBR, SiteRef.Create);
            _propertyMap.Add(Command.REQ_FOUNDSITE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_HIDDENSITE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_SITE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NEARBYSITE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_CLAIMEDTHRONE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_UNCLAIMEDTHRONE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_FULLOWNER, NationRef.Create);
            _propertyMap.Add(Command.REQ_DOMOWNER, NationRef.Create);
            _propertyMap.Add(Command.REQ_MYDOMINION, IntProperty.Create);
            _propertyMap.Add(Command.REQ_DOMINION, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXDOMINION, IntProperty.Create);
            _propertyMap.Add(Command.REQ_DOMCHANCE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_GODISMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_CHAOS, IntProperty.Create);
            _propertyMap.Add(Command.REQ_LAZY, IntProperty.Create);
            _propertyMap.Add(Command.REQ_COLD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_DEATH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_UNLUCK, IntProperty.Create);
            _propertyMap.Add(Command.REQ_UNMAGIC, IntProperty.Create);
            _propertyMap.Add(Command.REQ_ORDER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PROD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_HEAT, IntProperty.Create);
            _propertyMap.Add(Command.REQ_GROWTH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_LUCK, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAGIC, IntProperty.Create);
            _propertyMap.Add(Command.REQ_COMMANDER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MONSTER, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_2MONSTERS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_5MONSTERS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_NOMONSTER, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_MNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_NOMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_DEADMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_MINTROOPS, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXTROOPS, IntProperty.Create);
            _propertyMap.Add(Command.REQ_HUMANOIDRES, CommandProperty.Create);
            _propertyMap.Add(Command.REQ_RESEARCHER, CommandProperty.Create);
            _propertyMap.Add(Command.REQ_PREACH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHFIRE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHAIR, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHWATER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHEARTH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHASTRAL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHDEATH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHNATURE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHBLOOD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHHOLY, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHFIRE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHAIR, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHWATER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHEARTH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHASTRAL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHDEATH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHNATURE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHBLOOD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHHOLY, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOPATHALL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_TARGNOMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_TARGGOD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGPROPHET, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGHUMANOID, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMALE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGPATH1, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGPATH2, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGPATH3, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGPATH4, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOPATH1, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOPATH2, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOPATH3, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOPATH4, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGAFF, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGORDER, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGITEM, ItemRef.Create);
            _propertyMap.Add(Command.REQ_TARGNOITEM, ItemRef.Create);
            _propertyMap.Add(Command.REQ_TARGUNDEAD, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGDEMON, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGANIMAL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGINANIMATE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMINDLESS, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGIMMOBILE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMAGICBEING, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGOWNER, NationRef.Create);
            _propertyMap.Add(Command.REQ_TARGFOREIGNOK, CommandProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMINSIZE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMAXSIZE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_CODE, EventCodeRef.Create);
            _propertyMap.Add(Command.REQ_ANYCODE, EventCodeRef.Create);
            _propertyMap.Add(Command.REQ_NOTANYCODE, EventCodeRef.Create);
            _propertyMap.Add(Command.REQ_NEARBYCODE, EventCodeRef.Create);
            _propertyMap.Add(Command.REQ_NEAROWNCODE, EventCodeRef.Create);
            _propertyMap.Add(Command.REQ_PERMONTH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_NOENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_ENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_MYENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_FRIENDLYENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_HOSTILEENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_ENCHDOM, EnchIDRef.Create);
            _propertyMap.Add(Command.REQ_ARENADONE, IntProperty.Create);
            _propertyMap.Add(Command.NATION, NationOwnerRef.Create); //TODO
            _propertyMap.Add(Command.NATIONENCH, EnchIDRef.Create);
            _propertyMap.Add(Command.MSG, StringProperty.Create);
            _propertyMap.Add(Command.NOTEXT, CommandProperty.Create);
            _propertyMap.Add(Command.NOLOG, CommandProperty.Create);
            _propertyMap.Add(Command.MAGICITEM, IntProperty.Create);
            _propertyMap.Add(Command.GOLD, IntProperty.Create);
            _propertyMap.Add(Command.EXACTGOLD, IntProperty.Create);
            _propertyMap.Add(Command.INCSCALE, IntProperty.Create);
            _propertyMap.Add(Command.INCSCALE2, IntProperty.Create);
            _propertyMap.Add(Command.INCSCALE3, IntProperty.Create);
            _propertyMap.Add(Command.ZZ1D3VIS, IntProperty.Create);
            _propertyMap.Add(Command.ZZ1D6VIS, IntProperty.Create);
            _propertyMap.Add(Command.ZZ2D4VIS, IntProperty.Create);
            _propertyMap.Add(Command.ZZ2D6VIS, IntProperty.Create);
            _propertyMap.Add(Command.ZZ3D6VIS, IntProperty.Create);
            _propertyMap.Add(Command.DECSCALE, IntProperty.Create);
            _propertyMap.Add(Command.DECSCALE2, IntProperty.Create);
            _propertyMap.Add(Command.DECSCALE3, IntProperty.Create);
            _propertyMap.Add(Command.ZZ4D6VIS, IntProperty.Create);
            _propertyMap.Add(Command.GEMLOSS, IntProperty.Create);
            _propertyMap.Add(Command.LANDGOLD, IntProperty.Create);
            _propertyMap.Add(Command.LANDPROD, IntProperty.Create);
            _propertyMap.Add(Command.TAXBOOST, IntProperty.Create);
            _propertyMap.Add(Command.DEFENCE, IntProperty.Create);
            _propertyMap.Add(Command.KILL, IntProperty.Create);
            _propertyMap.Add(Command.KILLPOP, IntProperty.Create);
            _propertyMap.Add(Command.INCPOP, IntProperty.Create);
            _propertyMap.Add(Command.INCCORPSES, IntProperty.Create);
            _propertyMap.Add(Command.EMIGRATION, IntProperty.Create);
            _propertyMap.Add(Command.UNREST, IntProperty.Create);
            _propertyMap.Add(Command.INCDOM, IntProperty.Create);
            _propertyMap.Add(Command.FORT, IntProperty.Create);
            _propertyMap.Add(Command.TEMPLE, IntProperty.Create);
            _propertyMap.Add(Command.LAB, IntProperty.Create);
            _propertyMap.Add(Command.REVEALSITE, CommandProperty.Create);
            _propertyMap.Add(Command.ADDSITE, SiteRef.Create);
            _propertyMap.Add(Command.REMOVESITE, SiteRef.Create);
            _propertyMap.Add(Command.HIDDENSITE, SiteRef.Create);
            _propertyMap.Add(Command.VISITORS, CommandProperty.Create);
            _propertyMap.Add(Command.NEWDOM, IntProperty.Create);
            _propertyMap.Add(Command.REVOLT, CommandProperty.Create);
            _propertyMap.Add(Command.REVEALPROV, CommandProperty.Create);
            _propertyMap.Add(Command.CLAIMTHRONE, CommandProperty.Create);
            _propertyMap.Add(Command.ASSASSIN, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ASSOWNER, NationRef.Create);
            _propertyMap.Add(Command.STEALTHCOM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.COM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ2COM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ4COM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ5COM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.TEMPUNITS, IntProperty.Create);
            _propertyMap.Add(Command.ZZ1UNIT, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ1D3UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ2D3UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ3D3UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ4D3UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ1D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ2D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ3D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ4D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ5D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ6D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ7D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ8D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ9D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ10D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ11D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ12D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ13D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ14D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ15D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ZZ16D6UNITS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.STRIKEUNITS, IntProperty.Create);
            _propertyMap.Add(Command.KILLMON, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.KILL2D6MON, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.KILLCOM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.CURSE, IntProperty.Create);
            _propertyMap.Add(Command.DISEASE, IntProperty.Create);
            _propertyMap.Add(Command.RESEARCHAFF, BitmaskProperty.Create);
            _propertyMap.Add(Command.GAINAFF, BitmaskProperty.Create);
            _propertyMap.Add(Command.GAINMARK, CommandProperty.Create);
            _propertyMap.Add(Command.BANISHED, IntProperty.Create);
            _propertyMap.Add(Command.ADDEQUIP, IntProperty.Create);
            _propertyMap.Add(Command.TRANSFORM, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.POISON, IntProperty.Create);
            _propertyMap.Add(Command.XP, IntProperty.Create);
            _propertyMap.Add(Command.FIREBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.AIRBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.WATERBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.EARTHBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.ASTRALBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.DEATHBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.NATUREBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.BLOODBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.HOLYBOOST, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.PATHBOOST, IntProperty.Create);
            _propertyMap.Add(Command.WORLDINCSCALE, IntProperty.Create);
            _propertyMap.Add(Command.WORLDINCSCALE2, IntProperty.Create);
            _propertyMap.Add(Command.WORLDINCSCALE3, IntProperty.Create);
            _propertyMap.Add(Command.WORLDDECSCALE, IntProperty.Create);
            _propertyMap.Add(Command.WORLDDECSCALE2, IntProperty.Create);
            _propertyMap.Add(Command.WORLDDECSCALE3, IntProperty.Create);
            _propertyMap.Add(Command.WORLDUNREST, IntProperty.Create);
            _propertyMap.Add(Command.WORLDINCDOM, IntProperty.Create);
            _propertyMap.Add(Command.WORLDRITREBATE, IntProperty.Create);
            _propertyMap.Add(Command.WORLDDARKNESS, CommandProperty.Create);
            _propertyMap.Add(Command.WORLDCURSE, IntProperty.Create);
            _propertyMap.Add(Command.WORLDDISEASE, IntProperty.Create);
            _propertyMap.Add(Command.WORLDMARK, IntProperty.Create);
            _propertyMap.Add(Command.WORLDHEAL, IntProperty.Create);
            _propertyMap.Add(Command.WORLDAGE, IntProperty.Create);
            _propertyMap.Add(Command.LINGER, IntProperty.Create);
            _propertyMap.Add(Command.FLAGLAND, IntProperty.Create);
            _propertyMap.Add(Command.DELAY, IntProperty.Create);
            _propertyMap.Add(Command.DELAY25, IntProperty.Create);
            _propertyMap.Add(Command.DELAY50, IntProperty.Create);
            _propertyMap.Add(Command.DELAYSKIP, IntProperty.Create);
            _propertyMap.Add(Command.ORDER, BitmaskProperty.Create);
            _propertyMap.Add(Command.CODE, EventCodeRef.Create);
            _propertyMap.Add(Command.CODE2, EventCodeRef.Create);
            _propertyMap.Add(Command.RESETCODE, EventCodeRef.Create);
            _propertyMap.Add(Command.PURGECALENDAR, IntProperty.Create);
            _propertyMap.Add(Command.PURGEDELAYED, IntProperty.Create);
            _propertyMap.Add(Command.ID, EventEffectCodeRef.Create);
            _propertyMap.Add(Command.CODEDELAY, EventCodeRef.Create);
            _propertyMap.Add(Command.CODEDELAY2, EventCodeRef.Create);
            _propertyMap.Add(Command.RESETCODEDELAY, EventCodeRef.Create);
            _propertyMap.Add(Command.RESETCODEDELAY2, EventCodeRef.Create);
            _propertyMap.Add(Command.ARENA, CommandProperty.Create);
            _propertyMap.Add(Command.RESOLVEARENA1, CommandProperty.Create);
            _propertyMap.Add(Command.RESOLVEARENA2, CommandProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOAFF, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MINCORPSES, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MAXCORPSES, IntProperty.Create);
            _propertyMap.Add(Command.REQ_THRONESITE, IntProperty.Create);
            _propertyMap.Add(Command.SETPOPTYPE, PoptypeIDRef.Create);
            _propertyMap.Add(Command.REQ_TARGNOORDER, BitmaskProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMINMORALE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGMAXMORALE, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOTOWNER, NationRef.Create);
            _propertyMap.Add(Command.REQ_TARGNOTALLY, NationRef.Create);
            _propertyMap.Add(Command.REQ_TARGALLY, NationRef.Create);
            _propertyMap.Add(Command.REQ_GODISNOTMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_WORLDITEM, ItemRef.Create);
            _propertyMap.Add(Command.REQ_NOWORLDITEM, ItemRef.Create);
            _propertyMap.Add(Command.SETXP, IntProperty.Create);
            // Dominions 6 additions:
            _propertyMap.Add(Command.CLEAR, CommandProperty.Create); //#clear 
            _propertyMap.Add(Command.REQ_TURNRARE, IntProperty.Create); //#req_turnrare <value>
            _propertyMap.Add(Command.REQ_MONTH, IntProperty.Create); //#req_month <0 – 11>
            _propertyMap.Add(Command.REQ_AI, IntProperty.Create); //#req_ai <0 | 1>
            _propertyMap.Add(Command.REQ_NEARBYCAPITAL, IntProperty.Create); //#req_nearbycapital < 0 | 1 >
            _propertyMap.Add(Command.REQ_NOTPOPTYPE, IntProperty.Create); //#req_notpoptype <value>
            _propertyMap.Add(Command.REQ_VOIDOK, IntProperty.Create); //#req_voidok < 0 | 1 >
            _propertyMap.Add(Command.REQ_FORTID, IntProperty.Create); //#req_fortid <fort number>
            _propertyMap.Add(Command.REQ_PLANE, IntProperty.Create); //#req_plane < planenr >
            _propertyMap.Add(Command.REQ_KELP, IntProperty.Create); //#req_kelp < 0 | 1 >
            _propertyMap.Add(Command.REQ_GORGE, IntProperty.Create); //#req_gorge < 0 | 1 >
            _propertyMap.Add(Command.REQ_DEEP, IntProperty.Create); //#req_deep < 0 | 1 >
            _propertyMap.Add(Command.REQ_FORESTCAVE, IntProperty.Create); //#req_forestcave < 0 | 1 >
            _propertyMap.Add(Command.REQ_DRIP, IntProperty.Create); //#req_drip < 0 | 1 >
            _propertyMap.Add(Command.REQ_CRYSTAL, IntProperty.Create); //#req_crystal < 0 | 1 >
            _propertyMap.Add(Command.REQ_NATIVESOIL, CommandProperty.Create); //#req_nativesoil 
            _propertyMap.Add(Command.REQ_VOID, IntProperty.Create); //#req_void < 0 | 1 >
            _propertyMap.Add(Command.REQ_GODAWAKE, IntProperty.Create); //#req_godawake <0 - 1>
            _propertyMap.Add(Command.REQ_PRETISMNR, MonsterOrMontagRef.Create); // a monster or a tag (the game: -100000..19999, as #req_mnr)
            _propertyMap.Add(Command.REQ_PRETAWAKE, IntProperty.Create); //#req_pretawake <0 - 1>
            _propertyMap.Add(Command.REQ_PATHGLAMOUR, IntProperty.Create); //#req_pathglamour <level>
            _propertyMap.Add(Command.REQ_NOPATHGLAMOUR, IntProperty.Create); //#req_nopathglamour <level>
            _propertyMap.Add(Command.REQ_TARGSIGHT, IntProperty.Create); //#req_targsight < 0 | 1 >
            _propertyMap.Add(Command.REQ_TARGMANYGEMS, IntProperty.Create); //#req_targmanygems <path number>
            _propertyMap.Add(Command.REQ_TARGHORRORMARK, IntProperty.Create); //#req_targhorrormark <value>
            _propertyMap.Add(Command.REQ_TARGINSANE, IntProperty.Create); //#req_targinsane <0 | 1>
            _propertyMap.Add(Command.REQ_TARGSEDUCTIONS, IntProperty.Create); //#req_targseductions <value>
            _propertyMap.Add(Command.REQ_TARGMINKILLS, IntProperty.Create); //#req_targminkills <value>
            _propertyMap.Add(Command.REQ_TARGMAXKILLS, IntProperty.Create); //#req_targmaxkills <value>
            _propertyMap.Add(Command.REQ_NOTCODE, EventCodeRef.Create); //#req_notcode <event code>
            _propertyMap.Add(Command.REQ_ENCHTARGET, EnchIDRef.Create); //#req_enchtarget <ench nbr>
            _propertyMap.Add(Command.REQ_ENCHNEARBY, EnchIDRef.Create); //#req_enchnearby <ench nbr>
            _propertyMap.Add(Command.REQ_MINGLOBALS, IntProperty.Create); //#req_minglobals <nbr>
            _propertyMap.Add(Command.REQ_MAXGLOBALS, IntProperty.Create); //#req_maxglobals <nbr>
            _propertyMap.Add(Command.REQ_VARPOS, EventVarRef.Create); //#req_varpos <event var>
            _propertyMap.Add(Command.REQ_VARNEG, EventVarRef.Create); //#req_varneg <event var>
            _propertyMap.Add(Command.REQ_VARZERO, EventVarRef.Create); //#req_varzero <event var>
            _propertyMap.Add(Command.REQ_VARONE, EventVarRef.Create); //#req_varone <event var>
            _propertyMap.Add(Command.ASSOWNERENCH, EnchIDRef.Create); //#assownerench <ench nbr>
            _propertyMap.Add(Command.ASSFOLLOWER1, MonsterOrMontagRef.Create); //#assfollower1 name | <number>
            _propertyMap.Add(Command.ASSFOLLOWER2, MonsterOrMontagRef.Create); //#assfollower2 name | <number>
            _propertyMap.Add(Command.ASSFOLLOWER3, MonsterOrMontagRef.Create); //#assfollower3 name | <number>
            _propertyMap.Add(Command.ASSFOLLOWER1D3, MonsterOrMontagRef.Create); //#assfollower1d3 name | <number>
            _propertyMap.Add(Command.HEALAFF, IntProperty.Create); //#healaff <nbr>
            _propertyMap.Add(Command.FORCETRANSFORM, MonsterOrMontagRef.Create); //#forcetransform name | <number>
            _propertyMap.Add(Command.REMOUNT, CommandProperty.Create); //#remount 
            _propertyMap.Add(Command.CLEARTARG, CommandProperty.Create); //#cleartarg 
            _propertyMap.Add(Command.KILLTARG, CommandProperty.Create); //#killtarg 
            _propertyMap.Add(Command.ADDSEDUCTIONS, IntProperty.Create); //#addseductions <value>
            _propertyMap.Add(Command.ADDKILLS, IntProperty.Create); //#addkills <value>
            _propertyMap.Add(Command.GLAMOURBOOST, MonsterOrMontagRef.Create); //#glamourboost name | <number>
            _propertyMap.Add(Command.DISPGLOBALS, IntProperty.Create); //#dispglobals <str>
            _propertyMap.Add(Command.CLEARVAR, EventVarRef.Create); //#clearvar <event var>
            _propertyMap.Add(Command.INCVAR, EventVarRef.Create); //#incvar <event var>
            _propertyMap.Add(Command.DECVAR, EventVarRef.Create); //#decvar <event var>
            _propertyMap.Add(Command.INC10VAR, EventVarRef.Create); //#inc10var <event var>
            _propertyMap.Add(Command.DEC10VAR, EventVarRef.Create); //#dec10var <event var>
            _propertyMap.Add(Command.INVVAR, EventVarRef.Create); //#invvar <event var>
            _propertyMap.Add(Command.TOGGLEVAR, EventVarRef.Create); //#togglevar <event var>
            _propertyMap.Add(Command.ADDASCENSION, IntProperty.Create); //#addascension <AP>
            _propertyMap.Add(Command.MINASCENSION, IntProperty.Create); //#minascension <AP>
            _propertyMap.Add(Command.REQ_NEARBYTHRONE, IntProperty.Create); //#req_nearbythrone < 0 | 1 >
            _propertyMap.Add(Command.HEADER, IntProperty.Create); //#header <type>
            _propertyMap.Add(Command.FORCEGOLD, IntProperty.Create); //#forcegold <value>
            _propertyMap.Add(Command.FORCEEXACTGOLD, IntProperty.Create); //#forceexactgold <value>
            _propertyMap.Add(Command.FORCE1D3VIS, IntProperty.Create); //#force1d3vis <gem type>
            _propertyMap.Add(Command.FORCE1D6VIS, IntProperty.Create); //#force1d6vis <gem type>
            _propertyMap.Add(Command.FORCE2D4VIS, IntProperty.Create); //#force2d4vis <gem type>
            _propertyMap.Add(Command.FORCE2D6VIS, IntProperty.Create); //#force2d6vis <gem type>
            _propertyMap.Add(Command.FORCE3D6VIS, IntProperty.Create); //#force3d6vis <gem type>
            _propertyMap.Add(Command.FORCE4D6VIS, IntProperty.Create); //#force4d6vis <gem type>
            _propertyMap.Add(Command.GEMLOSSSMALL, IntProperty.Create); //#gemlosssmall <gem type>
            _propertyMap.Add(Command.GEMLOSSLARGE, IntProperty.Create); //#gemlosslarge <gem type>
            _propertyMap.Add(Command.ADDGEO, BitmaskProperty.Create); //#addgeo <terrain bitmask>
            _propertyMap.Add(Command.REMGEO, BitmaskProperty.Create); //#remgeo <terrain bitmask>
            // Additional Dom6 additions
            _propertyMap.Add(Command.REQ_TARGREALMNR, MonsterOrMontagRef.Create); // a monster (the game: 0..19999, as #req_targmnr)
            // Read by the Dominions 6.37 parser (tools/dom6exe catalog)
            _propertyMap.Add(Command.ARENA1, CommandProperty.Create);
            _propertyMap.Add(Command.ARENA2, CommandProperty.Create);
            _propertyMap.Add(Command.GAINAFFMOUNT, IntProperty.Create);
            _propertyMap.Add(Command.HEALAFFMOUNT, IntProperty.Create);
            _propertyMap.Add(Command.MAYBEADDSITE, SiteRef.Create);
            _propertyMap.Add(Command.MAYBEHIDDENSITE, SiteRef.Create);
            _propertyMap.Add(Command.NEWNBOR, IntProperty.Create);
            _propertyMap.Add(Command.REMNBOR, IntProperty.Create);
            _propertyMap.Add(Command.REQ_CAVEFOREST, IntProperty.Create);
            _propertyMap.Add(Command.REQ_DEEPSEA, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MINRESEARCH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_MNRBS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_MONSTERBS, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_NOREALMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_PATH, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PATHGEMS, IntProperty.Create);
            _propertyMap.Add(Command.REQ_PROVNBR, IntProperty.Create);
            _propertyMap.Add(Command.REQ_REALMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.REQ_SCHOOL, IntProperty.Create);
            _propertyMap.Add(Command.REQ_TARGNOREALMNR, MonsterOrMontagRef.Create);
            _propertyMap.Add(Command.VAR0UNITS, MonsterOrMontagRef.Create);
        }


        internal override void Assign(string value, string comment, Mod _parent, bool selected = false)
        {
            base.Assign(value, comment, _parent, selected);
            this.SetID(value, comment);
            ParentMod = _parent;
            Selected = selected;
            ParentMod.Events.Add(this);
        }

        internal override Command GetNewCommand()
        {
            return Command.NEWEVENT;
        }

        internal override Command GetSelectCommand()
        {
            return Command.SELECTEVENT;
        }

        public override Dictionary<Command, Func<Property>> GetPropertyMap()
        {
            return _propertyMap;
        }

        internal override EntityType GetEntityType()
        {
            return EntityType.EVENT;
        }

        public override bool TryGetCopyFrom(out IDEntity copy)
        {
            copy = null;
            return false;
        }
    }
}
