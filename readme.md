# Optimisation du nombre de requêtes UDP par seconde

Le tableau ci-dessous classe les pistes d'optimisation par priorité estimée. Les gains réels dépendent de la charge, du matériel et du type de requêtes ; il faut mesurer avant et après chaque changement.

| Priorité | Ligne(s) | Problème | À améliorer |
|---|---|---|---|
| 1 — Très haute | `detour.cs:260, 286` | `find` appelle `PathFinder` deux fois pour une requête : une première fois pour afficher le résultat, puis une seconde fois pour le retourner. Le calcul du chemin est donc répété. | Calculer le chemin une seule fois, conserver le résultat dans une variable et retourner cette variable. |
| 2 — Très haute | `UDP.cs:101-111` | La boucle de réception loue un buffer, alloue aussi un tableau inutilisé de 65 535 octets, puis alloue et copie les données reçues dans un autre tableau. Le buffer loué n'est jamais rendu au pool. | Supprimer le tableau inutilisé et rendre le buffer loué au pool dans un `finally` après la copie. Pour réduire davantage les allocations, transférer la propriété du buffer loué jusqu'au worker et le rendre au pool après traitement. |
| 3 — Haute | `UDP.cs:221-223, 269-271, 345` ; `CNaveMesh.cs:280, 317` | Des messages de console sont produits sur le chemin de traitement des requêtes. Les écritures console peuvent sérialiser les workers et ralentir le serveur. | Désactiver les logs de diagnostic par requête en fonctionnement normal. Les remplacer par des compteurs/métriques périodiques et ne conserver les logs détaillés que pour le diagnostic ou les erreurs. |
| 4 — Haute | `UDP.cs:45-49, 115-120` | La file autorise jusqu'à 500 000 paquets. Si le traitement ne suit pas la réception, les réponses prennent du retard ; quand la file est pleine, les nouveaux paquets sont rejetés. | Mesurer la profondeur de file et les rejets. Définir une capacité en fonction du délai acceptable et traiter explicitement la saturation, au lieu de laisser une longue file masquer le retard. |
| 5 — Moyenne | `UDP.cs:181, 439` | Le compteur `TPS` reflète les paquets traités, pas les réponses UDP confirmées comme envoyées. Les erreurs d'envoi sont attrapées dans `SendBufferedAsync`, donc une requête peut être comptée sans réponse réussie. | Suivre séparément les requêtes reçues, traitées, les réponses envoyées avec succès et les erreurs d'envoi. Mesurer aussi la durée du calcul et de l'envoi pour identifier le goulot réel. |
| 6 — À mesurer | `UDP.cs:269-290, 345-365` ; `detour.cs:292-301` ; `CNaveMesh.cs:303-324` | Les demandes `moveNPC` recherchent le sol au départ et à l'arrivée avant le calcul du trajet. Les demandes `navmesh` calculent le trajet sans ces recherches. Le coût dépend du type de requête et du navmesh. | Mesurer séparément la durée de `SearchValidFloor` et de `find`. Éviter une recherche uniquement si les positions sont déjà validées et que cela préserve le comportement attendu. |

## Ordre conseillé

Commencer par supprimer le calcul de chemin en double, puis corriger le cycle de vie des buffers de réception et réduire les logs par requête. Ensuite, surveiller la profondeur de file et mesurer les étapes séparément. Comparer les requêtes reçues et les réponses envoyées avec succès avant et après chaque modification.

## Test de charge UDP avec Bun

Le script `Stresscharge.ts` envoie des requêtes FlatBuffers compatibles avec le schéma `NavMesh`, puis compte les datagrammes de réponse reçus. Il ne valide pas individuellement chaque réponse et le ratio réponses/envoyés reste approximatif, car l'UDP ne fournit pas d'identifiant de corrélation exploité par ce test.

Installer la dépendance depuis ce dossier :

```powershell
bun install
```

Commencer par un test modéré sur une adresse privée :

```powershell
bun run Stresscharge.ts --host 192.168.1.61 --port 11000 --rate 1000 --duration 30 --type navmesh --zone test --start 0,0,0 --end 10,0,10
```

Augmenter progressivement `--rate`, puis tester jusqu'à 50000 requêtes/s uniquement sur une machine ou un réseau que vous contrôlez. La cible est limitée aux adresses IPv4 loopback et privées ; la durée est limitée à 300 secondes. Le type par défaut est `navmesh`. Les coordonnées doivent correspondre à une zone valide de la carte. Pour les tests `spawn`, vérifier d'abord que le serveur initialise correctement la liste `_threadOffsets` avant son accès dans cette branche.

Exemple de progression : 100, 500, 1000, 5000, 10000, 25000 puis 50000 requêtes/s, avec 30 à 60 secondes de mesure par palier. Le script affiche les datagrammes envoyés acceptés par le socket client et les réponses reçues ; comparer ces résultats avec le CPU, la file d'attente et les erreurs côté serveur.