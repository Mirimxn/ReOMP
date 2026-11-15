# Writes omp-data/character-previews/preview-bases.txt: the Characters rows usable as "previewBase", grouped by a guess from names and paths.
# Usage: python character_preview_bases.py <game>/omp-custom-vehicles.log <mod>/mod.json  (writes preview-bases.txt here)
import re,sys,json,collections
L=open(sys.argv[1],encoding='utf-8',errors='replace').read().splitlines()
mj=json.load(open(sys.argv[2]))
rows=[]
on=False
for l in L:
    if "All 'Characters' entries" in l: on=True; continue
    if on:
        m=re.match(r'^\[[\d:]+\]   (\d+): (.*?) / (.*?) / (.*?) / ?(.*)$',l)
        if not m: break
        rows.append(dict(i=int(m[1]),name=m[2],key=m[3],scene=m[5].strip()))
used=collections.defaultdict(list)
for a in mj['actions']:
    p=a.get('payload',{})
    if 'previewBase' in p and p.get('tableName')=='Characters': used[p['previewBase'].lower()].append(p['id'])
byscene=collections.defaultdict(list)
for r in rows:
    if r['scene']: byscene[r['scene'].lower()].append(r['name'])
def group(r):
    n=r['name'].upper();s=r['scene'].lower()
    if r['key'].startswith('VEHICLE_') or '/vehicles/' in s: return None
    droid='/droids/' in s or 'cheapdroid' in s
    if re.search(r'C3PO|TC14|TC\d|K3PO|U3PO|PROTOCOL|AP5|ME8D9|EV9D9|RA7|CZ\d|0_0_0|TRIPLEZERO|000',n): return 'Protocol droids'
    if re.search(r'ASTROMECH|^(R\d|C1|BB\d|BB_|BB9|DO$|D_O|CHOPPER|QT|R1|UTILITY)',n) or re.search(r'/(r\d\w*|bb\w*|chopper|c1\w*)/',s) and droid: return 'Astromech droids'
    if droid or re.search(r'DROID|^IG\d|^IG_|^2_1B|^21B|^4LOM|MOUSE|MSE6|GONK|PROBE',n): return 'Other droids'
    if re.search(r'CLONE|^ARC|CAPTAINREX|^REX|CODY|FIVES|ECHO|WOLFFE|GREE|BLY|BACARA|BOIL|WAXER|JESSE|^TUP|KIX|HARDCASE|DOGMA|NEYO|APPO|THIRE|^FOX|HUNTER|WRECKER|TECH$|CROSSHAIR|OMEGA',n): return 'Clones'
    if re.search(r'FSO|FIRSTORDER|FIRST_ORDER|KYLO|PHASMA|HUX|PRAETORIAN|SITHTROOPER|SITH_TROOPER',n): return 'First Order and Sith Eternal'
    if re.search(r'STORM|SANDTROOPER|SNOWTROOPER|SCOUT|DEATHTROOPER|SHORETROOPER|TIEPILOT|TIE_PILOT|IMPERIAL|PURGE|SHADOW|MAGMA|DARKTROOPER|ROYALGUARD|INQUISITOR|EMPEROR|VADER|TARKIN|KRENNIC|VEERS|PIETT|OZZEL|NEEDA|THRAWN|AT_?AT|ATST|OFFICER',n): return 'Empire'
    if re.search(r'REBEL|RESISTANCE|XWING|YWING|AWING|BWING|PILOT|HOTH|ENDOR|ACKBAR|ANTILLES|MONMOTHMA|DODONNA|CRYNYD|BIGGS|WEDGE|PORKINS|DUTCH|POE|FINN|ROSE|HOLDO|CONNIX|LEIA',n): return 'Rebels and Resistance'
    if re.search(r'JEDI|SKYWALKER|ANAKIN|LUKE|^BEN|OBIWAN|KENOBI|YODA|WINDU|MACE|KIADIMUNDI|PLOKOON|SHAAKTI|AAYLA|KITFISTO|LUMINARA|BARRISS|AHSOKA|QUIGON|KANAN|EZRA|CALKESTIS|REY',n): return 'Jedi and Force users'
    if re.search(r'BATTLEDROID|DROIDEKA|SUPER|DOOKU|GRIEVOUS|MAUL|NUTE|GUNRAY|JANGO|ZAM|VENTRESS|SAVAGE|SEPARATIST|GEONOSIAN|POGGLE',n): return 'Separatists and Sith'
    if re.search(r'BOBA|FETT|BOSSK|DENGAR|IG88|ZUCKUSS|GREEDO|EMBO|CADBANE|BOUNTY|MANDO|MANDALORIAN|DINDJARIN|DIN_|BOKATAN|FENNEC|CARA|KUIIL|GROGU',n): return 'Bounty hunters and Mandalorians'
    if '/small/' in s: return 'Small figures (children, Jawas, Ewoks…)'
    if '/creature' in s: return 'Creatures'
    if '/minifig/' in s: return 'Other minifigs'
    return 'Big figures and others'
groups=collections.OrderedDict((g,[]) for g in ['Protocol droids','Astromech droids','Other droids','Clones','Empire','First Order and Sith Eternal','Rebels and Resistance','Jedi and Force users','Separatists and Sith','Bounty hunters and Mandalorians','Other minifigs','Small figures (children, Jawas, Ewoks…)','Creatures','Big figures and others'])
noscene=0
for r in rows:
    if not r['scene']: noscene+=1;continue
    g=group(r)
    if g: groups[g].append(r)
out=[]
out.append('Characters whose preview slot can be used as "previewBase" for a custom character (Characters table).')
out.append('Made from the roster list in omp-custom-vehicles.log ('+str(len(rows))+' rows; '+str(noscene)+' rows without a preview scene are left out, as are vehicles).')
out.append('')
out.append('How to choose:')
out.append('- Give every custom character its own base. Entries that load the same preview scene count as one base (marked "same scene as ...").')
out.append('- Pick characters you rarely look at: the base\'s own preview shows the custom one while it is in use.')
out.append('- A base shaped like the custom character (a minifig for a minifig, a droid for a droid) is the safer choice.')
out.append('- The groups below are guessed from names and file paths, not read from the game\'s class data.')
out.append('')
for g,rs in groups.items():
    if not rs: continue
    out.append(g+' ('+str(len(rs))+'):')
    for r in sorted(rs,key=lambda r:r['name'].lower()):
        same=[n for n in byscene[r['scene'].lower()] if n!=r['name']]
        tag=[]
        if same: tag.append('same scene as '+', '.join(same[:4])+(' …' if len(same)>4 else ''))
        if r['name'].lower() in used: tag.append('<- used by '+', '.join(used[r['name'].lower()]))
        out.append('  '+r['name'].ljust(40)+' '+r['scene']+('  ['+'; '.join(tag)+']' if tag else ''))
    out.append('')
open('preview-bases.txt','w',encoding='utf-8',newline='\r\n').write('\n'.join(out))
print({g:len(v) for g,v in groups.items()},noscene,len(rows))
