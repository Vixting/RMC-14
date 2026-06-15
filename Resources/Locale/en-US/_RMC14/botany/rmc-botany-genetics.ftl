# Flora Data Disc
rmc-flora-disc-empty = The disc is blank.
rmc-flora-disc-source = Gene source: { $source }
rmc-flora-disc-gene-entry = • [color=cyan]{ $gene }[/color]
rmc-flora-disc-wipe-verb = Wipe disc
rmc-flora-disc-wiped = You wipe the disc clean.

# Gene type labels
rmc-gene-type-products = Products
rmc-gene-type-products-desc = chemicals, harvest type
rmc-gene-type-consumption = Consumption
rmc-gene-type-consumption-desc = water & nutrient use
rmc-gene-type-environment = Environment
rmc-gene-type-environment-desc = heat, light & pressure
rmc-gene-type-resistance = Resistance
rmc-gene-type-resistance-desc = weed, pest & toxin tolerance
rmc-gene-type-vigour = Vigour
rmc-gene-type-vigour-desc = yield, lifespan & growth rate
rmc-gene-type-flowers = Flowers
rmc-gene-type-flowers-desc = plant appearance

# Lysis-Isolation Centrifuge — popups
rmc-lysis-centrifuge-no-power = The centrifuge has no power.
rmc-lysis-centrifuge-no-seed-data = This seed packet contains no genetic data.
rmc-lysis-centrifuge-seedless = This plant is seedless — its genome cannot be extracted.
rmc-lysis-centrifuge-seed-already-inserted = A seed packet is already loaded. Eject it first.
rmc-lysis-centrifuge-seed-inserted = Seed packet inserted. Press 'Process Genome' to scan it.
rmc-lysis-centrifuge-no-seed-inserted = No seed packet is loaded.
rmc-lysis-centrifuge-already-loaded = A genome is already loaded. Clear the buffer first.
rmc-lysis-centrifuge-scanned = Genome of [color=green]{ $plant }[/color] loaded. Buffer degradation: 0%.
rmc-lysis-centrifuge-no-genome = No genome is loaded. Insert a seed packet first.
rmc-lysis-centrifuge-degraded = Buffer fully degraded. Genome wiped.
rmc-lysis-centrifuge-disc-already-loaded = A disc is already loaded. Eject it first via the interface.
rmc-lysis-centrifuge-disc-inserted = Flora data disc inserted.
rmc-lysis-centrifuge-no-disc = No disc loaded. Insert a flora data disc first.
rmc-lysis-centrifuge-disc-has-gene = This sequence is already stored on the disc.
rmc-lysis-centrifuge-extracted = [color=cyan]{ $gene }[/color] extracted. Buffer degradation: { $degradation }/{ $max }.
rmc-lysis-centrifuge-buffer-wiped = Buffer fully degraded — genome lost.
rmc-lysis-centrifuge-clear-verb = Clear buffer
rmc-lysis-centrifuge-cleared = Genetic buffer cleared.

# Lysis-Isolation Centrifuge — UI
rmc-centrifuge-window-title = Lysis-Isolation Centrifuge
rmc-centrifuge-seed-header = SEED PACKET
rmc-centrifuge-no-seed = [No seed loaded]
rmc-centrifuge-seed-loaded = [Seed inserted]
rmc-centrifuge-eject-seed = Eject Seed
rmc-centrifuge-process-seed = Process Genome
rmc-centrifuge-genome-header = GENOME BUFFER
rmc-centrifuge-no-genome = [No genome loaded]
rmc-centrifuge-degradation-label = Degradation:
rmc-centrifuge-degradation-value = { $current }/{ $max }
rmc-centrifuge-clear-buffer = Clear Buffer
rmc-centrifuge-disc-label = Disc:
rmc-centrifuge-disc-loaded = [Disc inserted]
rmc-centrifuge-disc-empty = [No disc]
rmc-centrifuge-eject-disc = Eject Disc
rmc-centrifuge-genes-header = GENE SEQUENCES
rmc-centrifuge-gene-label = { $code } - { $type }:
rmc-centrifuge-gene-label-copied = { $code } - { $type }: ✓
rmc-centrifuge-extract-gene = Extract gene
rmc-centrifuge-gene-already-copied = This gene sequence is already on the disc.

# Bioballistic Delivery System — popups
rmc-gene-editor-no-power = The delivery system has no power.
rmc-gene-editor-disc-already-loaded = A disc is already loaded. Eject it first.
rmc-gene-editor-load-disc-first = Load a flora data disc first.
rmc-gene-editor-seed-already-loaded = A seed packet is already loaded. Eject it first.
rmc-gene-editor-disc-loaded = Disc loaded.
rmc-gene-editor-seed-loaded = Seed packet loaded.
rmc-gene-editor-apply-verb = Apply genes
rmc-gene-editor-eject-verb = Eject contents
rmc-gene-editor-disc-empty = The loaded disc has no genes to apply.
rmc-gene-editor-no-seed-data = This seed packet contains no genetic data.
rmc-gene-editor-seed-ruined = This seed has been modified too many times and can no longer accept genetic changes.
rmc-gene-editor-failed = [color=red]Gene delivery failed! The seed is ruined.[/color]
rmc-gene-editor-applied = Genes applied successfully. Remaining edit tolerance: { $remaining }%.

# Bioballistic Delivery System — UI
rmc-gene-editor-window-title = Bioballistic Delivery System
rmc-gene-editor-disc-header = FLORA DATA DISC
rmc-gene-editor-no-disc = [No disc loaded]
rmc-gene-editor-disc-present = [Disc loaded]
rmc-gene-editor-disc-source = Source: { $source }
rmc-gene-editor-disc-no-genes = No genes stored on disc.
rmc-gene-editor-gene-entry = • { $gene }
rmc-gene-editor-seed-header = TARGET SEED
rmc-gene-editor-no-seed = [No seed loaded]
rmc-gene-editor-seed-present = { $name }
rmc-gene-editor-seed-present-unknown = [Unknown seed]
rmc-gene-editor-edit-count-label = Edit tolerance:
rmc-gene-editor-edit-count-value = { $current }/{ $max }
rmc-gene-editor-apply-button = Apply Genes
rmc-gene-editor-eject-button = Eject All
