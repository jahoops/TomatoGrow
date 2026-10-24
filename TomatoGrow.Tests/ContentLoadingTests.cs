using System;
using System.Collections.Generic;
using System.IO;
using TomatoGrow.GameContent;
using TomatoGrow.GameContent.Definitions;
using Xunit;

namespace TomatoGrow.Tests;

public sealed class ContentLoadingTests
{
    [Fact]
        public void LoadFromModules_BaseGameOnly_LoadsTurnip()
        {
                string modulesPath = CreateTempModules(
                        baseGameItemsJson: """
                        [
                            {
                                "id": "turnip_seed",
                                "name": "Turnip Seeds",
                                "type": "seed",
                                "buyPrice": 10,
                                "sellPrice": 1
                            },
                            {
                                "id": "turnip",
                                "name": "Turnip",
                                "type": "crop",
                                "buyPrice": 0,
                                "sellPrice": 30
                            }
                        ]
                        """,
                        baseGameCropsJson: """
                        [
                            {
                                "id": "turnip",
                                "seedItemId": "turnip_seed",
                                "harvestItemId": "turnip",
                                "growthDays": 4
                            }
                        ]
                        """);

                try
                {
                        ContentModuleLoader loader = new();
                        ContentRegistry registry = loader.LoadFromModules(modulesPath);

                        Assert.True(registry.ItemsById.ContainsKey("turnip_seed"));
                        Assert.True(registry.CropsById.ContainsKey("turnip"));
                        Assert.False(registry.ItemsById.ContainsKey("potato_seed"));
                        Assert.False(registry.CropsById.ContainsKey("potato"));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

        [Fact]
        public void LoadFromModules_PotatoPackOnly_LoadsPotato()
        {
                string modulesPath = CreateTempModules(
                        potatoPackItemsJson: """
                        [
                            {
                                "id": "potato_seed",
                                "name": "Potato Seeds",
                                "type": "seed",
                                "buyPrice": 12,
                                "sellPrice": 1
                            },
                            {
                                "id": "potato",
                                "name": "Potato",
                                "type": "crop",
                                "buyPrice": 0,
                                "sellPrice": 35
                            }
                        ]
                        """,
                        potatoPackCropsJson: """
                        [
                            {
                                "id": "potato",
                                "seedItemId": "potato_seed",
                                "harvestItemId": "potato",
                                "growthDays": 5
                            }
                        ]
                        """);

                try
                {
                        ContentModuleLoader loader = new();
                        ContentRegistry registry = loader.LoadFromModules(modulesPath);

                        Assert.True(registry.ItemsById.ContainsKey("potato_seed"));
                        Assert.True(registry.CropsById.ContainsKey("potato"));
                        Assert.False(registry.ItemsById.ContainsKey("turnip_seed"));
                        Assert.False(registry.CropsById.ContainsKey("turnip"));
                        Assert.Equal(5, registry.GetCrop("potato").GrowthDays);
                        Assert.Equal(35, registry.GetItem("potato").SellPrice);
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

        [Fact]
        public void LoadFromModules_LoadsBothBaseGameAndPotatoPack()
    {
        ContentModuleLoader loader = new();
        string modulesPath = Path.Combine(AppContext.BaseDirectory, "ContentModules");

        ContentRegistry registry = loader.LoadFromModules(modulesPath);

        Assert.True(registry.ItemsById.ContainsKey("turnip_seed"));
        Assert.True(registry.CropsById.ContainsKey("turnip"));
        Assert.True(registry.ItemsById.ContainsKey("potato_seed"));
        Assert.True(registry.CropsById.ContainsKey("potato"));

        ItemDefinition potatoItem = registry.GetItem("potato");
        CropDefinition potatoCrop = registry.GetCrop("potato");

        Assert.Equal(35, potatoItem.SellPrice);
        Assert.Equal(5, potatoCrop.GrowthDays);
        Assert.True(registry.TryGetCropBySeedItemId("potato_seed", out CropDefinition potatoFromSeed));
        Assert.Equal("potato", potatoFromSeed.Id);
        Assert.True(registry.ItemsById.ContainsKey("grape_seed"));
        Assert.True(registry.ItemsById.ContainsKey("hops_seed"));
        Assert.True(registry.CropsById.ContainsKey("grape"));
        Assert.True(registry.CropsById.ContainsKey("hops"));
        Assert.True(registry.ItemsById.ContainsKey("aged_wine"));
        Assert.True(registry.MachinesById.ContainsKey("fermenter"));
        Assert.True(registry.MachinesById.ContainsKey("aging_barrel"));
        Assert.True(registry.ProcessingRecipesById.ContainsKey("grape_to_wine"));
        Assert.True(registry.ProcessingRecipesById.ContainsKey("hops_to_beer"));
        Assert.True(registry.ProcessingRecipesById.ContainsKey("wine_to_aged_wine"));
        Assert.True(registry.MarketOffersById.ContainsKey("buy_grapes"));
        Assert.True(registry.MarketOffersById.ContainsKey("buy_hops"));
        Assert.True(registry.MarketOffersById.ContainsKey("buy_wine"));
        Assert.True(registry.RestorationSitesById.ContainsKey("old_junk_pile"));
        Assert.True(registry.TryGetCropBySeedItemId("grape_seed", out CropDefinition grapeFromSeed));
        Assert.Equal("grape", grapeFromSeed.Id);
        Assert.True(registry.TryGetCropBySeedItemId("hops_seed", out CropDefinition hopsFromSeed));
        Assert.Equal("hops", hopsFromSeed.Id);
        Assert.Equal("processor_machine", registry.GetMachine("fermenter").Type);
        Assert.Equal("processor_machine", registry.GetMachine("aging_barrel").Type);
        Assert.Equal(2, registry.GetProcessingRecipesForMachine("fermenter").Count);
        Assert.Single(registry.GetProcessingRecipesForMachine("aging_barrel"));
        Assert.Equal("grape", registry.GetMarketOffer("buy_grapes").ItemId);
        Assert.Equal(3, registry.GetMarketOffer("buy_hops").Quantity);
        Assert.Equal(190, registry.GetMarketOffer("buy_wine").Price);
                Assert.Equal("unlock_expansion_level", registry.GetRestorationSite("old_junk_pile").RewardType);
                Assert.Equal(TurnipTestState.MaxFarmExpansionLevel, registry.GetRestorationSite("old_junk_pile").RewardValue);
    }

    [Fact]
    public void InMemoryRegistry_WithoutRestorationDefinitions_DoesNotSynthesizeOldJunkPile()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed", BuyPrice = 10, SellPrice = 1 },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop", BuyPrice = 0, SellPrice = 30 }
            ],
            [
                new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }
            ]);

        Assert.Empty(registry.RestorationSites);
        Assert.False(registry.RestorationSitesById.ContainsKey(TurnipTestState.OldJunkPileId));
    }

    [Fact]
    public void Validate_ReportsMissingRequiredOldJunkPileForExpansionGate()
    {
        string modulesPath = CreateTempModules(
            baseGameItemsJson: """
            [
              { "id": "turnip_seed", "name": "Turnip Seeds", "type": "seed", "buyPrice": 10, "sellPrice": 1 },
              { "id": "turnip", "name": "Turnip", "type": "crop", "buyPrice": 0, "sellPrice": 30 }
            ]
            """,
            baseGameCropsJson: """
            [
              { "id": "turnip", "seedItemId": "turnip_seed", "harvestItemId": "turnip", "growthDays": 4 }
            ]
            """);

        try
        {
            ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
            ContentValidationResult result = new ContentValidator().Validate(registry);

            Assert.Contains("Missing required restoration site 'old_junk_pile' for expansion gate.", result.Errors);
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }

        [Fact]
        public void Validate_ReportsDuplicateRestorationSiteIdAcrossDifferentModules()
        {
                string modulesPath = CreateTempModules(
                        baseGameRestorationSitesJson: """
                        [
                            {
                                "id": "old_junk_pile",
                                "name": "Old Junk Pile",
                                "description": "Base blocker.",
                                "cost": 500,
                                "rewardType": "unlock_expansion_level",
                                "rewardValue": 2,
                                "rewardDescription": "Unlocks 5x3 farm expansion"
                            }
                        ]
                        """,
                        potatoPackRestorationSitesJson: """
                        [
                            {
                                "id": "old_junk_pile",
                                "name": "Duplicate blocker",
                                "description": "Duplicate blocker.",
                                "cost": 600,
                                "rewardType": "unlock_expansion_level",
                                "rewardValue": 2,
                                "rewardDescription": "Unlocks 5x3 farm expansion"
                            }
                        ]
                        """);

                try
                {
                        ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
                        ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Duplicate restoration site id 'old_junk_pile'.", StringComparison.Ordinal)
                                && error.Contains("BaseGame", StringComparison.Ordinal)
                                && error.Contains("PotatoPack", StringComparison.Ordinal)
                                && error.Contains("restoration_sites.json", StringComparison.Ordinal));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

        [Fact]
        public void Validate_ReportsInvalidRestorationSiteRewardType()
        {
                string modulesPath = CreateTempModules(
                        baseGameRestorationSitesJson: """
                        [
                            {
                                "id": "old_junk_pile",
                                "name": "Old Junk Pile",
                                "description": "Base blocker.",
                                "cost": 500,
                                "rewardType": "unlock_spaceship",
                                "rewardValue": 2,
                                "rewardDescription": "Unlocks 5x3 farm expansion"
                            }
                        ]
                        """);

                try
                {
                        ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
                        ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Restoration site 'old_junk_pile'", StringComparison.Ordinal)
                                && error.Contains("invalid RewardType 'unlock_spaceship'", StringComparison.Ordinal)
                                && error.Contains("restoration_sites.json", StringComparison.Ordinal));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

    [Fact]
    public void Validate_FermentationPackRecipeReferences_AreValid()
    {
        ContentModuleLoader loader = new();
        string modulesPath = Path.Combine(AppContext.BaseDirectory, "ContentModules");

        ContentRegistry registry = loader.LoadFromModules(modulesPath);
        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.DoesNotContain(result.Errors, error => error.Contains("grape_to_wine", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, error => error.Contains("hops_to_beer", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, error => error.Contains("wine_to_aged_wine", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, error => error.Contains("buy_grapes", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, error => error.Contains("buy_hops", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Errors, error => error.Contains("buy_wine", StringComparison.Ordinal));
        Assert.Equal("wine", registry.GetProcessingRecipe("grape_to_wine").OutputItemId);
        Assert.Equal("beer", registry.GetProcessingRecipe("hops_to_beer").OutputItemId);
        Assert.Equal("aged_wine", registry.GetProcessingRecipe("wine_to_aged_wine").OutputItemId);
    }

    [Fact]
    public void Validate_ReportsInvalidMarketOfferItemReference()
    {
        string modulesPath = CreateTempModules(
            baseGameItemsJson: """
            [
              { "id": "turnip_seed", "name": "Turnip Seeds", "type": "seed", "buyPrice": 10, "sellPrice": 1 }
            ]
            """,
            baseGameMarketOffersJson: """
            [
              { "id": "buy_ghost", "itemId": "ghost_grape", "quantity": 2, "price": 99, "category": "raw_input" }
            ]
            """);

        try
        {
            ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
            ContentValidationResult result = new ContentValidator().Validate(registry);

            Assert.Contains(result.Errors, error =>
                error.Contains("Market offer 'buy_ghost'", StringComparison.Ordinal)
                && error.Contains("missing ItemId 'ghost_grape'", StringComparison.Ordinal)
                && error.Contains("market_offers.json", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }

    [Fact]
    public void Validate_ReportsDuplicateMachineIdAcrossDifferentModules()
    {
        string modulesPath = CreateTempModules(
            baseGameMachinesJson: """
            [
              {
                "id": "fermenter",
                "name": "Base Fermenter",
                "type": "processor_machine"
              }
            ]
            """,
            potatoPackMachinesJson: """
            [
              {
                "id": "fermenter",
                "name": "Potato Fermenter",
                "type": "processor_machine"
              }
            ]
            """);

        try
        {
            ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
            ContentValidationResult result = new ContentValidator().Validate(registry);

            Assert.Contains(result.Errors, error =>
                error.Contains("Duplicate machine id 'fermenter'.", StringComparison.Ordinal)
                && error.Contains("BaseGame", StringComparison.Ordinal)
                && error.Contains("PotatoPack", StringComparison.Ordinal)
                && error.Contains("machines.json", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }

    [Fact]
    public void Validate_ReportsDuplicateProcessingRecipeIdAcrossDifferentModules()
    {
        string modulesPath = CreateTempModules(
            baseGameItemsJson: """
            [
              { "id": "turnip", "name": "Turnip", "type": "crop", "buyPrice": 0, "sellPrice": 30 },
              { "id": "wine", "name": "Wine", "type": "processed_good", "buyPrice": 0, "sellPrice": 160 }
            ]
            """,
            baseGameMachinesJson: """
            [
              { "id": "fermenter", "name": "Fermenter", "type": "processor_machine" }
            ]
            """,
            baseGameProcessingRecipesJson: """
            [
              {
                "id": "turnip_recipe",
                "machineId": "fermenter",
                "inputItemId": "turnip",
                "inputQuantity": 1,
                "outputItemId": "wine",
                "outputQuantity": 1,
                "processingDays": 1
              }
            ]
            """,
            potatoPackItemsJson: """
            [
              { "id": "potato", "name": "Potato", "type": "crop", "buyPrice": 0, "sellPrice": 35 },
              { "id": "beer", "name": "Beer", "type": "processed_good", "buyPrice": 0, "sellPrice": 140 }
            ]
            """,
            potatoPackMachinesJson: """
            [
              { "id": "keg", "name": "Keg", "type": "processor_machine" }
            ]
            """,
            potatoPackProcessingRecipesJson: """
            [
              {
                "id": "turnip_recipe",
                "machineId": "keg",
                "inputItemId": "potato",
                "inputQuantity": 1,
                "outputItemId": "beer",
                "outputQuantity": 1,
                "processingDays": 1
              }
            ]
            """);

        try
        {
            ContentRegistry registry = new ContentModuleLoader().LoadFromModules(modulesPath);
            ContentValidationResult result = new ContentValidator().Validate(registry);

            Assert.Contains(result.Errors, error =>
                error.Contains("Duplicate processing recipe id 'turnip_recipe'.", StringComparison.Ordinal)
                && error.Contains("BaseGame", StringComparison.Ordinal)
                && error.Contains("PotatoPack", StringComparison.Ordinal)
                && error.Contains("processing_recipes.json", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }

    [Fact]
    public void Validate_ReportsMissingMachineReferenceInProcessingRecipe()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop" },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good" }
            ],
            [],
            [],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                }
            ]);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("missing MachineId 'fermenter'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsMissingInputItemReferenceInProcessingRecipe()
    {
        ContentRegistry registry = new(
            [new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good" }],
            [],
            [new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" }],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                }
            ]);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("missing InputItemId 'grape'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsMissingOutputItemReferenceInProcessingRecipe()
    {
        ContentRegistry registry = new(
            [new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop" }],
            [],
            [new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" }],
            [
                new ProcessingRecipeDefinition
                {
                    Id = "grape_to_wine",
                    MachineId = "fermenter",
                    InputItemId = "grape",
                    InputQuantity = 3,
                    OutputItemId = "wine",
                    OutputQuantity = 1,
                    ProcessingDays = 2
                }
            ]);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("missing OutputItemId 'wine'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsInvalidInputQuantityInProcessingRecipe()
    {
        ContentValidationResult result = ValidateInvalidProcessingRecipe(
            new ProcessingRecipeDefinition
            {
                Id = "grape_to_wine",
                MachineId = "fermenter",
                InputItemId = "grape",
                InputQuantity = 0,
                OutputItemId = "wine",
                OutputQuantity = 1,
                ProcessingDays = 2
            });

        Assert.Contains(result.Errors, error => error.Contains("invalid InputQuantity 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsInvalidOutputQuantityInProcessingRecipe()
    {
        ContentValidationResult result = ValidateInvalidProcessingRecipe(
            new ProcessingRecipeDefinition
            {
                Id = "grape_to_wine",
                MachineId = "fermenter",
                InputItemId = "grape",
                InputQuantity = 3,
                OutputItemId = "wine",
                OutputQuantity = 0,
                ProcessingDays = 2
            });

        Assert.Contains(result.Errors, error => error.Contains("invalid OutputQuantity 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsInvalidProcessingDaysInProcessingRecipe()
    {
        ContentValidationResult result = ValidateInvalidProcessingRecipe(
            new ProcessingRecipeDefinition
            {
                Id = "grape_to_wine",
                MachineId = "fermenter",
                InputItemId = "grape",
                InputQuantity = 3,
                OutputItemId = "wine",
                OutputQuantity = 1,
                ProcessingDays = 0
            });

        Assert.Contains(result.Errors, error => error.Contains("invalid ProcessingDays 0", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsDuplicateItemId()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop" },
                new ItemDefinition { Id = "turnip", Name = "Turnip Again", Type = "crop" }
            ],
            []);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("Duplicate item id 'turnip'.", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsMissingHarvestItem()
    {
        ContentRegistry registry = new(
            [new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed" }],
            [new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }]);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("missing HarvestItemId 'turnip'", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ReportsInvalidGrowthDays()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed" },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop" }
            ],
            [new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 0 }]);

        ContentValidationResult result = new ContentValidator().Validate(registry);

        Assert.Contains(result.Errors, error => error.Contains("invalid GrowthDays 0", StringComparison.Ordinal));
    }

    [Fact]
    public void TryGetCropBySeedItemId_FindsMatchingCrop()
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "turnip_seed", Name = "Turnip Seeds", Type = "seed" },
                new ItemDefinition { Id = "turnip", Name = "Turnip", Type = "crop" }
            ],
            [new CropDefinition { Id = "turnip", SeedItemId = "turnip_seed", HarvestItemId = "turnip", GrowthDays = 4 }]);

        bool found = registry.TryGetCropBySeedItemId("turnip_seed", out CropDefinition crop);

        Assert.True(found);
        Assert.Equal("turnip", crop.Id);
    }

    [Fact]
    public void Validate_ReportsDuplicateItemIdAcrossDifferentModules()
    {
        string modulesPath = CreateTempModules(
            baseGameItemsJson: """
            [
              {
                "id": "turnip_seed",
                "name": "Turnip Seeds",
                "type": "seed",
                "buyPrice": 10,
                "sellPrice": 1
              }
            ]
            """,
            potatoPackItemsJson: """
            [
              {
                "id": "turnip_seed",
                "name": "Duplicated Seed",
                "type": "seed",
                "buyPrice": 99,
                "sellPrice": 99
              }
            ]
            """);

        try
        {
            ContentModuleLoader loader = new();
            ContentRegistry registry = loader.LoadFromModules(modulesPath);

            ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Duplicate item id 'turnip_seed'.", StringComparison.Ordinal) &&
                                error.Contains("BaseGame", StringComparison.Ordinal) &&
                                error.Contains("PotatoPack", StringComparison.Ordinal) &&
                                error.Contains("items.json", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(modulesPath, recursive: true);
        }
    }

        [Fact]
        public void Validate_ReportsDuplicateCropIdAcrossDifferentModulesWithSourceContext()
        {
                string modulesPath = CreateTempModules(
                        baseGameItemsJson: """
                        [
                            {
                                "id": "turnip_seed",
                                "name": "Turnip Seeds",
                                "type": "seed",
                                "buyPrice": 10,
                                "sellPrice": 1
                            },
                            {
                                "id": "turnip",
                                "name": "Turnip",
                                "type": "crop",
                                "buyPrice": 0,
                                "sellPrice": 30
                            }
                        ]
                        """,
                        baseGameCropsJson: """
                        [
                            {
                                "id": "root_crop",
                                "seedItemId": "turnip_seed",
                                "harvestItemId": "turnip",
                                "growthDays": 4
                            }
                        ]
                        """,
                        potatoPackItemsJson: """
                        [
                            {
                                "id": "potato_seed",
                                "name": "Potato Seeds",
                                "type": "seed",
                                "buyPrice": 12,
                                "sellPrice": 1
                            }
                        ]
                        """,
                        potatoPackCropsJson: """
                        [
                            {
                                "id": "root_crop",
                                "seedItemId": "potato_seed",
                                "harvestItemId": "potato",
                                "growthDays": 5
                            }
                        ]
                        """);

                try
                {
                        ContentModuleLoader loader = new();
                        ContentRegistry registry = loader.LoadFromModules(modulesPath);

                        ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Duplicate crop id 'root_crop'.", StringComparison.Ordinal) &&
                                error.Contains("BaseGame", StringComparison.Ordinal) &&
                                error.Contains("PotatoPack", StringComparison.Ordinal) &&
                                error.Contains("crops.json", StringComparison.Ordinal));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

        [Fact]
        public void Validate_MissingHarvestItem_IncludesCropIdAndSourceContext()
        {
                string modulesPath = CreateTempModules(
                        potatoPackItemsJson: """
                        [
                            {
                                "id": "potato_seed",
                                "name": "Potato Seeds",
                                "type": "seed",
                                "buyPrice": 12,
                                "sellPrice": 1
                            }
                        ]
                        """,
                        potatoPackCropsJson: """
                        [
                            {
                                "id": "potato",
                                "seedItemId": "potato_seed",
                                "harvestItemId": "potato",
                                "growthDays": 5
                            }
                        ]
                        """);

                try
                {
                        ContentModuleLoader loader = new();
                        ContentRegistry registry = loader.LoadFromModules(modulesPath);

                        ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Crop 'potato'", StringComparison.Ordinal) &&
                                error.Contains("references missing HarvestItemId 'potato'", StringComparison.Ordinal) &&
                                error.Contains("PotatoPack", StringComparison.Ordinal) &&
                                error.Contains("crops.json", StringComparison.Ordinal));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

        [Fact]
        public void Validate_InvalidGrowthDays_IncludesCropIdAndSourceContext()
        {
                string modulesPath = CreateTempModules(
                        potatoPackItemsJson: """
                        [
                            {
                                "id": "potato_seed",
                                "name": "Potato Seeds",
                                "type": "seed",
                                "buyPrice": 12,
                                "sellPrice": 1
                            },
                            {
                                "id": "potato",
                                "name": "Potato",
                                "type": "crop",
                                "buyPrice": 0,
                                "sellPrice": 35
                            }
                        ]
                        """,
                        potatoPackCropsJson: """
                        [
                            {
                                "id": "potato",
                                "seedItemId": "potato_seed",
                                "harvestItemId": "potato",
                                "growthDays": 0
                            }
                        ]
                        """);

                try
                {
                        ContentModuleLoader loader = new();
                        ContentRegistry registry = loader.LoadFromModules(modulesPath);

                        ContentValidationResult result = new ContentValidator().Validate(registry);

                        Assert.Contains(result.Errors, error =>
                                error.Contains("Crop 'potato'", StringComparison.Ordinal) &&
                                error.Contains("invalid GrowthDays 0", StringComparison.Ordinal) &&
                                error.Contains("PotatoPack", StringComparison.Ordinal) &&
                                error.Contains("crops.json", StringComparison.Ordinal));
                }
                finally
                {
                        Directory.Delete(modulesPath, recursive: true);
                }
        }

    private static string CreateTempModules(
        string baseGameItemsJson = null,
        string baseGameCropsJson = null,
        string potatoPackItemsJson = null,
        string potatoPackCropsJson = null,
        string baseGameMarketOffersJson = null,
        string potatoPackMarketOffersJson = null,
        string fermentationPackItemsJson = null,
        string fermentationPackCropsJson = null,
        string fermentationPackMachinesJson = null,
        string fermentationPackProcessingRecipesJson = null,
        string fermentationPackMarketOffersJson = null,
        string baseGameMachinesJson = null,
        string baseGameProcessingRecipesJson = null,
        string potatoPackMachinesJson = null,
        string potatoPackProcessingRecipesJson = null,
        string baseGameRestorationSitesJson = null,
        string potatoPackRestorationSitesJson = null,
        string fermentationPackRestorationSitesJson = null)
    {
        string modulesPath = Path.Combine(Path.GetTempPath(), $"tomatogrow-content-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(modulesPath);

        if (baseGameItemsJson is not null || baseGameCropsJson is not null || baseGameMachinesJson is not null || baseGameProcessingRecipesJson is not null || baseGameMarketOffersJson is not null || baseGameRestorationSitesJson is not null)
        {
            string baseGamePath = Path.Combine(modulesPath, "BaseGame");
            Directory.CreateDirectory(baseGamePath);

            if (baseGameItemsJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "items.json"), baseGameItemsJson);

            if (baseGameCropsJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "crops.json"), baseGameCropsJson);

            if (baseGameMachinesJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "machines.json"), baseGameMachinesJson);

            if (baseGameProcessingRecipesJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "processing_recipes.json"), baseGameProcessingRecipesJson);

            if (baseGameMarketOffersJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "market_offers.json"), baseGameMarketOffersJson);

            if (baseGameRestorationSitesJson is not null)
                File.WriteAllText(Path.Combine(baseGamePath, "restoration_sites.json"), baseGameRestorationSitesJson);
        }

        if (potatoPackItemsJson is not null || potatoPackCropsJson is not null || potatoPackMachinesJson is not null || potatoPackProcessingRecipesJson is not null || potatoPackMarketOffersJson is not null || potatoPackRestorationSitesJson is not null)
        {
            string potatoPackPath = Path.Combine(modulesPath, "PotatoPack");
            Directory.CreateDirectory(potatoPackPath);

            if (potatoPackItemsJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "items.json"), potatoPackItemsJson);

            if (potatoPackCropsJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "crops.json"), potatoPackCropsJson);

            if (potatoPackMachinesJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "machines.json"), potatoPackMachinesJson);

            if (potatoPackProcessingRecipesJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "processing_recipes.json"), potatoPackProcessingRecipesJson);

            if (potatoPackMarketOffersJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "market_offers.json"), potatoPackMarketOffersJson);

            if (potatoPackRestorationSitesJson is not null)
                File.WriteAllText(Path.Combine(potatoPackPath, "restoration_sites.json"), potatoPackRestorationSitesJson);
        }

        if (fermentationPackItemsJson is not null || fermentationPackCropsJson is not null || fermentationPackMachinesJson is not null || fermentationPackProcessingRecipesJson is not null || fermentationPackMarketOffersJson is not null || fermentationPackRestorationSitesJson is not null)
        {
            string fermentationPackPath = Path.Combine(modulesPath, "FermentationPack");
            Directory.CreateDirectory(fermentationPackPath);

            if (fermentationPackItemsJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "items.json"), fermentationPackItemsJson);

            if (fermentationPackCropsJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "crops.json"), fermentationPackCropsJson);

            if (fermentationPackMachinesJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "machines.json"), fermentationPackMachinesJson);

            if (fermentationPackProcessingRecipesJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "processing_recipes.json"), fermentationPackProcessingRecipesJson);

            if (fermentationPackMarketOffersJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "market_offers.json"), fermentationPackMarketOffersJson);

            if (fermentationPackRestorationSitesJson is not null)
                File.WriteAllText(Path.Combine(fermentationPackPath, "restoration_sites.json"), fermentationPackRestorationSitesJson);
        }

        return modulesPath;
    }

    private static ContentValidationResult ValidateInvalidProcessingRecipe(ProcessingRecipeDefinition processingRecipe)
    {
        ContentRegistry registry = new(
            [
                new ItemDefinition { Id = "grape", Name = "Grapes", Type = "crop" },
                new ItemDefinition { Id = "wine", Name = "Wine", Type = "processed_good" }
            ],
            [],
            [new MachineDefinition { Id = "fermenter", Name = "Fermenter", Type = "processor_machine" }],
            [processingRecipe]);

        return new ContentValidator().Validate(registry);
    }
}