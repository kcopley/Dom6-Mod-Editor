#modname "Renumber test"
#description "Dom5Tests fixture for the Renumber tool (docs/RENUMBER.md): every way a mod refers to its own numbers"

-- weapons: one on a game number (825, the game's Beak), one with a comment and spacing to keep,
-- one another's secondary effect
#newweapon 825
#name "Test Bile"
#dmg 5
#end

#newweapon 1500     -- the blade
#name "Test Blade"
#dmg 7
#secondaryeffect 1501
#end

#newweapon 1501
#name "Test Spark"
#dmg 3
#end

#newarmor 600
#name "Test Plate"
#prot 10
#end

-- a hydra: 5100 shrinks into 5101, which grows back into 5100: they move together
#newmonster 5100
#copystats 20
#name "Test Hydra"
#weapon 1500
#weapon 825
#shrinkhp 50
#end

#newmonster 5101
#copystats 20
#name "Test Hydra Small"
#growhp 30
#end

-- 5102 turns into 5103 with experience
#newmonster 5102
#copystats 20
#name "Test Squire"
#xpshape 100
#weapon "Test Blade"
#armor 600
#end

#newmonster 5103
#copystats 20
#name "Test Knight"
#end

-- 5110 shrinks into 5111, which isn't the mod's: it keeps its number, and 5111 stays free
#newmonster 5110
#copystats 20
#name "Test Lone"
#shrinkhp 50
#end

-- a name the game's Militia (30) has too: the name finds the game's, before and after
#newmonster 5120
#copystats 30
#name "Militia"
#end

-- a weapon line a later #clearweapons takes out (still read, still written), and two commands on a line
#newmonster 5130
#copystats 20
#name "Test Commander"
#weapon 1500
#clearweapons
#weapon 1501 #armor 600
#end

-- a later block of a unit of the mod's
#selectmonster 5103
#hp 20
#end

#newsite 1500
#name "Test Grove"
#path 6
#level 1
#rarity 5
#homemon 5102
#homecom 5130
#end

#newspell
#name "Test Call Hydra"
#school 5
#researchlevel 1
#path 0 6
#pathlevel 0 1
#effect 10001
#damage 5100
#nreff 1
#fatiguecost 100
#end

#newitem
#name "Test Flask"
#type 8
#constlevel 2
#mainpath 0
#mainlevel 1
#weapon 825
#end

-- a band by name, one with its number ("Name (id)": the game's lookup takes that form)
#newmerc
#name "Test Band"
#bossname "Test Boss"
#com "Test Commander (5130)"
#unit "Test Squire"
#nrunits 10
#level 1
#minmen 5
#minpay 100
#xp 10
#end

#selectnation 150
#name "Test Nation"
#era 2
#addrecunit 5102
#addreccom "Test Commander"
#startcom 5130
#end

#newtemplate 150
#form "Test Hydra (5100)"
#prison 0
#end

-- an event: units by number, and a number nothing has (5104) in the new block's way: it stays free
#newevent
#rarity 5
#req_monster 5100
#req_nomonster 5104
#msg "Test event."
#1unit 5102
#killcom 5130
#addsite 1500
#end
