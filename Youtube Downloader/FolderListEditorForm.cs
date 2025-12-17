namespace Youtube_Downloader;

public class FolderListEditorForm : Form
{
    private ListBox folderListBox = null!;
    private TextBox newFolderTextBox = null!;
    private Button addButton = null!;
    private Button removeButton = null!;
    private Button saveButton = null!;
    private Button cancelButton = null!;
    private Label countLabel = null!;

    private readonly FolderHistory folderHistory;
    private List<string> editingFolders = new();

    public bool ChangesMade { get; private set; } = false;

    public FolderListEditorForm(FolderHistory folderHistory)
    {
        this.folderHistory = folderHistory;
        InitializeComponents();
        LoadFolders();
    }

    private void InitializeComponents()
    {
        Text = "Folder List Editor";
        Size = new Size(400, 450);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var instructionLabel = new Label
        {
            Text = "Manage your folder name history. These appear in the folder dropdown lists.",
            Location = new Point(12, 12),
            Size = new Size(360, 35),
            AutoSize = false
        };

        folderListBox = new ListBox
        {
            Location = new Point(12, 50),
            Size = new Size(360, 250),
            SelectionMode = SelectionMode.One,
            Sorted = true
        };
        folderListBox.SelectedIndexChanged += FolderListBox_SelectedIndexChanged;

        countLabel = new Label
        {
            Text = $"0 / {FolderHistory.MaxFolders} folders",
            Location = new Point(12, 305),
            Size = new Size(150, 20)
        };

        var newFolderLabel = new Label
        {
            Text = "Add new folder:",
            Location = new Point(12, 330),
            AutoSize = true
        };

        newFolderTextBox = new TextBox
        {
            Location = new Point(12, 350),
            Size = new Size(280, 23)
        };
        newFolderTextBox.KeyDown += (s, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                AddButton_Click(s, e);
            }
        };

        addButton = new Button
        {
            Text = "Add",
            Location = new Point(300, 349),
            Size = new Size(72, 25)
        };
        addButton.Click += AddButton_Click;

        removeButton = new Button
        {
            Text = "Remove Selected",
            Location = new Point(12, 380),
            Size = new Size(110, 25),
            Enabled = false
        };
        removeButton.Click += RemoveButton_Click;

        saveButton = new Button
        {
            Text = "Save",
            Location = new Point(210, 380),
            Size = new Size(75, 25),
            DialogResult = DialogResult.OK
        };
        saveButton.Click += SaveButton_Click;

        cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(295, 380),
            Size = new Size(75, 25),
            DialogResult = DialogResult.Cancel
        };

        Controls.Add(instructionLabel);
        Controls.Add(folderListBox);
        Controls.Add(countLabel);
        Controls.Add(newFolderLabel);
        Controls.Add(newFolderTextBox);
        Controls.Add(addButton);
        Controls.Add(removeButton);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
    }

    private void LoadFolders()
    {
        editingFolders = folderHistory.GetSortedFolderNames(false); // Always show alphabetically in editor
        RefreshListBox();
    }

    private void RefreshListBox()
    {
        folderListBox.Items.Clear();
        foreach (var folder in editingFolders.OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            folderListBox.Items.Add(folder);
        }
        UpdateCountLabel();
    }

    private void UpdateCountLabel()
    {
        countLabel.Text = $"{editingFolders.Count} / {FolderHistory.MaxFolders} folders";
    }

    private void FolderListBox_SelectedIndexChanged(object? sender, EventArgs e)
    {
        removeButton.Enabled = folderListBox.SelectedIndex >= 0;
    }

    private void AddButton_Click(object? sender, EventArgs e)
    {
        string newFolder = newFolderTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(newFolder))
        {
            return;
        }

        // Sanitize folder name
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            newFolder = newFolder.Replace(c, '_');
        }

        // Check for duplicates
        if (editingFolders.Any(f => f.Equals(newFolder, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("This folder name already exists in the list.",
                "Duplicate", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Check max count
        if (editingFolders.Count >= FolderHistory.MaxFolders)
        {
            MessageBox.Show($"Maximum of {FolderHistory.MaxFolders} folders reached. Remove some folders first.",
                "Limit Reached", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        editingFolders.Add(newFolder);
        RefreshListBox();
        newFolderTextBox.Clear();
        newFolderTextBox.Focus();

        // Select the newly added item
        int index = folderListBox.Items.IndexOf(newFolder);
        if (index >= 0)
        {
            folderListBox.SelectedIndex = index;
        }
    }

    private void RemoveButton_Click(object? sender, EventArgs e)
    {
        if (folderListBox.SelectedItem is string selectedFolder)
        {
            editingFolders.RemoveAll(f => f.Equals(selectedFolder, StringComparison.OrdinalIgnoreCase));
            RefreshListBox();
        }
    }

    private void SaveButton_Click(object? sender, EventArgs e)
    {
        folderHistory.UpdateList(editingFolders);
        ChangesMade = true;
    }
}
