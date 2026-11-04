# Imagens dos detalhes dos locais

Atualização: 30 de outubro de 2026.

As imagens originais tinham 172 x 172 pixels. Foram mantidas como miniaturas da listagem. As novas imagens de detalhes têm 1536 x 1024 pixels e são reconstruções ilustrativas por IA, não fotografias documentais do campus. A geração usou a ferramenta integrada de imagens, sem CLI ou chave de API.

Arquivos finais em `CampusRating/Resources/Images/`:

- `campus_library_detail.png`, referência `campus_library.jpg`, ambiente `library`.
- `campus_bathrooms_detail.png`, referência `campus_bathrooms.jpg`, ambiente `bathrooms`.
- `campus_canteen_detail.png`, referência `campus_canteen.jpg`, ambiente `canteen`.
- `campus_laboratory_detail.png`, referência `campus_laboratory.jpg`, ambiente `laboratory`.

Cada imagem foi gerada em uma chamada separada, usando seu arquivo original como alvo de edição. O prompt foi o seguinte, substituindo `{ambiente}` pelo valor indicado acima:

> Use case: precise-object-edit. Asset type: high-resolution illustrative campus interior photo for a mobile app detail banner. The attached image is the EDIT TARGET, a 172x172 thumbnail of the {ambiente}. Reconstruct this same scene at high resolution with clean photographic detail. Preserve its architecture, viewpoint, spatial arrangement, main furniture, materials, colors and natural lighting as closely as possible. Output a single landscape 1536x1024 image; extend the side edges naturally only as needed, retaining the main composition. Remove the baked rounded thumbnail corners; the photo must fill the complete rectangular canvas edge to edge. Do not add people, text, labels, logos, watermarks or UI. Do not radically redesign the room. This is an illustrative app image, not a documentary photograph; improve clarity without exaggerated sharpening or oversaturated colors.

A tela `DetalhesLocalPage` seleciona a versão de maior resolução, e o projeto usa `Resize="False"` para essas imagens. Os nomes originais retornados pela API e os dados do banco permanecem os mesmos.
