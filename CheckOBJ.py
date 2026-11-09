
with open("./bin/debug/net9.0/public/landlow.obj", "r") as f:
    for line_num, line in enumerate(f, 1):
        if line.startswith('f '):
            parts = line.strip().split()
            for part in parts[1:]:
                if '/' in part:
                    idx = part.split('/')[0]
                else:
                    idx = part
                if idx == '0' or not idx.isdigit():
                    print(f"Ligne {line_num} : ERREUR - Indice invalide '{part}'")
                    exit()
print("Aucune erreur trouvée dans le fichier OBJ.")

#D:\CSharp\pathfinder\bin\Debug\net9.0\public\landlow.obj
# with open("./bin/debug/net9.0/public/landlow.obj", "r") as f:
#     for i, line in enumerate(f):
#         if line.startswith("f "):
#             parts = line.strip().split()
#             if len(parts) != 4:  # 3 indices + 'f'
#                 print(f"Ligne {i+1}: Face non triangulaire")
#             if any(not p.isdigit() or int(p) < 1 for p in parts[1:]):
#                 print(f"Ligne {i+1}: Indice de face invalide")