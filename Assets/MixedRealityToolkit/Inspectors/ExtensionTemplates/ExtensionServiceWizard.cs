﻿using Microsoft.MixedReality.Toolkit.Utilities;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Microsoft.MixedReality.Toolkit.Editor
{
    public partial class ExtensionServiceWizard : EditorWindow
    {
        private static ExtensionServiceWizard window;
        private static readonly Color enabledColor = Color.white;
        private static readonly Color disabledColor = Color.gray;
        private static readonly Color readOnlyColor = Color.Lerp(enabledColor, Color.clear, 0.5f);

        private ExtensionServiceCreator creator = new ExtensionServiceCreator();
        private List<string> errors = new List<string>();

        // These are stored prior to compilation to ensure results are not wiped out
        private ExtensionServiceCreator.CreateResult result;
        private List<string> resultOutput = new List<string>();
        // Ellipses display
        private int numEllipses = 0;

        [MenuItem("Mixed Reality Toolkit/Create Extension Service...", false, 1)]
        private static void CreateExtensionServiceMenuItem()
        {
            if (window != null)
            {
                Debug.Log("Only one window allowed at a time");
                // Only allow one window at a time
                return;
            }

            window = EditorWindow.CreateInstance<ExtensionServiceWizard>();
            window.titleContent = new GUIContent("Create Extension Service");
            window.ResetCreator();
            window.Show(true);
        }

        private void ResetCreator()
        {
            if (creator == null)
                creator = new ExtensionServiceCreator();

            creator.ResetState();
        }

        private void OnEnable()
        {
            Debug.Log("Initializing ExtensionServiceWizard window");
            if (creator == null)
                creator = new ExtensionServiceCreator();

            creator.LoadStoredState();
        }

        private void OnGUI()
        {
            if (!creator.ValidateAssets(errors))
            {
                EditorGUILayout.LabelField("Validating assets...", EditorStyles.miniLabel);
                foreach (string error in errors)
                {
                    EditorGUILayout.HelpBox(error, MessageType.Error);
                }
                return;
            }

            switch (creator.Stage)
            {
                case ExtensionServiceCreator.CreationStage.SelectNameAndPlatform:
                    DrawSelectNameAndPlatform();
                    break;

                case ExtensionServiceCreator.CreationStage.ChooseOutputFolders:
                    DrawChooseOutputFolders();
                    break;

                case ExtensionServiceCreator.CreationStage.CreatingExtensionService:
                case ExtensionServiceCreator.CreationStage.CreatingProfileInstance:
                    DrawCreatingAssets();
                    break;

                case ExtensionServiceCreator.CreationStage.Finished:
                    DrawFinished();
                    break;
            }
        }

        private void DrawSelectNameAndPlatform()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choose a name for your service.", EditorStyles.miniLabel);

            creator.ServiceName = EditorGUILayout.TextField("Service Name", creator.ServiceName);

            bool readyToProgress = creator.ValidateName(errors);
            foreach (string error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choose which platforms your service will support.", EditorStyles.miniLabel);

            creator.Platforms = (SupportedPlatforms)EditorGUILayout.EnumFlagsField("Platforms", creator.Platforms);
            readyToProgress &= creator.ValidatePlatforms(errors);
            foreach (string error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Choose a namespace for your service.", EditorStyles.miniLabel);

            creator.Namespace = EditorGUILayout.TextField("Namespace", creator.Namespace);
            readyToProgress &= creator.ValidateNamespace(errors);
            foreach (string error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            GUILayout.FlexibleSpace();

            GUI.color = readyToProgress ? enabledColor : disabledColor;
            if (GUILayout.Button("Next") && readyToProgress)
            {
                creator.Stage = ExtensionServiceCreator.CreationStage.ChooseOutputFolders;
                creator.StoreState();
            }
        }

        private void DrawChooseOutputFolders()
        {
            GUI.color = enabledColor;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Below are the files you will be generating", EditorStyles.miniLabel);

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(creator.ServiceName + ".cs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This is the main script for your service. It functions simliarly to a MonoBehaviour, with Enable, Disable and Update functions.", EditorStyles.wordWrappedMiniLabel);
            creator.ServiceFolderObject = EditorGUILayout.ObjectField("Target Folder", creator.ServiceFolderObject, typeof(UnityEngine.Object), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(creator.InterfaceName + ".cs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("This is the interface that other scripts will use to interact with your service.", EditorStyles.wordWrappedMiniLabel);
            creator.InterfaceFolderObject = EditorGUILayout.ObjectField("Target Folder", creator.InterfaceFolderObject, typeof(UnityEngine.Object), false);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(creator.ProfileName + ".cs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("An optional profile script for your service. Profiles are scriptable objects that store permanent config data. If you're not sure whether your service will need a profile, it's best to create one. You can remove it later.", EditorStyles.wordWrappedMiniLabel);
            creator.UsesProfile = EditorGUILayout.Toggle("Generate Profile", creator.UsesProfile);
            if (creator.UsesProfile)
            {
                creator.ProfileFolderObject = EditorGUILayout.ObjectField("Target Folder", creator.ProfileFolderObject, typeof(UnityEngine.Object), false);
            }
            EditorGUILayout.EndVertical();

            if (creator.UsesProfile)
            {
                EditorGUILayout.Space();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField(creator.ProfileAssetName + ".asset", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("A default instance of your profile.", EditorStyles.wordWrappedMiniLabel);
                creator.ProfileAssetFolderObject = EditorGUILayout.ObjectField("Target Folder", creator.ProfileAssetFolderObject, typeof(UnityEngine.Object), false);
                EditorGUILayout.EndVertical();
            }

            GUI.color = enabledColor;
            EditorGUILayout.Space();

            bool readyToProgress = creator.ValidateFolders(errors);
            foreach (string error in errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            GUILayout.FlexibleSpace();

            GUI.color = enabledColor;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Back"))
            {
                creator.Stage = ExtensionServiceCreator.CreationStage.SelectNameAndPlatform;
                creator.StoreState();
            }
            GUI.color = readyToProgress ? enabledColor : disabledColor;
            if (GUILayout.Button("Next") && readyToProgress)
            {
                // Start the async method that will wait for the service to be created
                CreateAssetsAsync();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawCreatingAssets()
        {
            EditorGUILayout.LabelField("Creating assets...", EditorStyles.boldLabel);

            // Draw crude working indicator so we know it hasn't frozen
            numEllipses++;
            if (numEllipses > 10)
                numEllipses = 0;

            string workingIndicator = ".";
            for (int i = 0; i < numEllipses; i++)
                workingIndicator += ".";

            EditorGUILayout.LabelField(workingIndicator, EditorStyles.boldLabel);

            switch (creator.Result)
            {
                case ExtensionServiceCreator.CreateResult.Error:
                    EditorGUILayout.HelpBox("There were errors while creating assets.", MessageType.Error);
                    break;

                default:
                    break;
            }

            foreach (string info in creator.CreationLog)
            {
                EditorGUILayout.LabelField(info, EditorStyles.wordWrappedMiniLabel);
            }

            Repaint();
        }

        private void DrawFinished()
        {
            EditorGUILayout.Space();

            switch (creator.Result)
            {
                case ExtensionServiceCreator.CreateResult.Successful:
                    break;

                case ExtensionServiceCreator.CreateResult.Error:
                    EditorGUILayout.HelpBox("There were errors during the creation process:", MessageType.Error);
                    foreach (string info in creator.CreationLog)
                    {
                        EditorGUILayout.LabelField(info, EditorStyles.wordWrappedMiniLabel);
                    }

                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Close"))
                    {
                        creator.ResetState();
                        Close();
                    }
                    // All done, bail early
                    return;
            }

            EditorGUILayout.HelpBox("Your service scripts have been created. Would you like to register this service in your current MixedRealityToolkit profile?", MessageType.Info);

            // Check to see whether it's possible ot register the profile
            bool canRegisterProfile = true;
            if (MixedRealityToolkit.Instance == null || !MixedRealityToolkit.Instance.HasActiveProfile)
            {
                EditorGUILayout.HelpBox("Toolkit has no active profile. Can't register service.", MessageType.Warning);
                canRegisterProfile = false;
            }
            else if (MixedRealityToolkit.Instance.ActiveProfile.RegisteredServiceProvidersProfile == null)
            {
                EditorGUILayout.HelpBox("Toolkit has no RegisteredServiceProvidersProfile. Can't register service.", MessageType.Warning);
                canRegisterProfile = false;
            }

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            GUI.color = canRegisterProfile ? enabledColor : disabledColor;
            if (GUILayout.Button("Register") && canRegisterProfile)
            {
                RegisterServiceWithActiveMixedRealityProfile();
                creator.ResetState();
                Close();
            }
            GUI.color = enabledColor;
            if (GUILayout.Button("Not Now"))
            {
                creator.ResetState();
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void RegisterServiceWithActiveMixedRealityProfile()
        {
            // We assume this has already been validated
            MixedRealityRegisteredServiceProvidersProfile servicesProfile = MixedRealityToolkit.Instance.ActiveProfile.RegisteredServiceProvidersProfile;
            // Use serialized object so this process can be undone
            SerializedObject servicesProfileObject = new SerializedObject(servicesProfile);
            SerializedProperty configurations = servicesProfileObject.FindProperty("configurations");
            int numConfigurations = configurations.arraySize;
            // Insert a new configuration at the end
            configurations.InsertArrayElementAtIndex(numConfigurations);
            // Get that config value
            SerializedProperty newConfig = configurations.GetArrayElementAtIndex(numConfigurations);

            // Configurations look like so:
            /*
                SystemType componentType;
                string componentName;
                uint priority;
                SupportedPlatforms runtimePlatform;
                BaseMixedRealityProfile configurationProfile;
            */

            SerializedProperty componentType = newConfig.FindPropertyRelative("componentType");
            SerializedProperty componentTypeReference = componentType.FindPropertyRelative("reference");
            SerializedProperty componentName = newConfig.FindPropertyRelative("componentName");
            SerializedProperty priority = newConfig.FindPropertyRelative("priority");
            SerializedProperty runtimePlatform = newConfig.FindPropertyRelative("runtimePlatform");
            SerializedProperty configurationProfile = newConfig.FindPropertyRelative("configurationProfile");

            componentTypeReference.stringValue = creator.ServiceType.AssemblyQualifiedName;
            // Add spaces between camel case service name
            componentName.stringValue = System.Text.RegularExpressions.Regex.Replace(creator.ServiceName, "(\\B[A-Z])", " $1");
            configurationProfile.objectReferenceValue = creator.ProfileInstance;
            runtimePlatform.intValue = (int)creator.Platforms;

            servicesProfileObject.ApplyModifiedProperties();

            // Select the profile so we can see what we've done
            Selection.activeObject = servicesProfile;
        }

        private async void CreateAssetsAsync()
        {
            await creator.BeginAssetCreationProcess();
        }
    }
}