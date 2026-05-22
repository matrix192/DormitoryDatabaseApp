using DormitoryDatabaseApp.Data;
using DormitoryDatabaseApp.Models;
using System.Data;
using System.Windows;
using System.Linq;
using System.Data;


namespace DormitoryDatabaseApp
{
    public partial class MainWindow : Window
    {
        private readonly DbService _dbService;
        private DataTable? _currentTable;

        public MainWindow()
        {
            InitializeComponent();

            _dbService = new DbService();

            LoadTables();
        }

        private void LoadTables()
        {
            var tables = new List<TableInfo>
            {
                new TableInfo
                {
                    DisplayName = "Факультет",
                    TableName = "факультет"
                },
                new TableInfo
                {
                    DisplayName = "Обслуживающий персонал",
                    TableName = "Обслуживающий персонал"
                },
                new TableInfo
                {
                    DisplayName = "Жилые помещения",
                    TableName = "жилые помещения"
                },
                new TableInfo
                {
                    DisplayName = "Общаги",
                    TableName = "общаги"
                },
                new TableInfo
                {
                    DisplayName = "Общежития-персонал",
                    TableName = "общежития-персонал"
                },
                new TableInfo
                {
                    DisplayName = "Факультет-общежитие",
                    TableName = "факультет-общежитие"
                }
            };

            TablesComboBox.ItemsSource = tables;
            TablesComboBox.SelectedIndex = 0;
        }

        private TableInfo? GetSelectedTable()
        {
            return TablesComboBox.SelectedItem as TableInfo;
        }

        private void LoadData()
        {
            try
            {
                var selectedTable = GetSelectedTable();

                if (selectedTable == null)
                {
                    MessageBox.Show("Выберите таблицу.");
                    return;
                }

                _currentTable = _dbService.GetAll(selectedTable.TableName);
                MainDataGrid.ItemsSource = _currentTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка загрузки данных:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            if (_currentTable != null)
            {
                _currentTable.DefaultView.RowFilter = "";
            }
        }

        private int GetMaxLength(string tableName, string columnName)
        {
            return tableName switch
            {
                "факультет" => columnName switch
                {
                    "Название" => 30,
                    "Адрес" => 100,
                    "Декан" => 30,
                    _ => 0
                },

                "Обслуживающий персонал" => columnName switch
                {
                    "Должность" => 100,
                    "Фамилия" => 30,
                    "Имя" => 20,
                    "Отчество" => 20,
                    _ => 0
                },

                "общаги" => columnName switch
                {
                    "адрес" => 100,
                    "Контакты" => 50,
                    "тип жилого помещения" => 15,
                    _ => 0
                },

                _ => 0
            };
        }

        private void TablesComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void GetAllButton_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void GetByIdButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTable = GetSelectedTable();

                if (selectedTable == null)
                {
                    MessageBox.Show("Выберите таблицу.");
                    return;
                }

                string idColumn = GetPrimaryKeyColumn(selectedTable.TableName);

                string input = Microsoft.VisualBasic.Interaction.InputBox(
                    $"Введите значение {idColumn}:",
                    "Поиск записи по ID",
                    ""
                );

                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show("ID не может быть пустым.");
                    return;
                }

                if (!int.TryParse(input, out int idValue) || idValue <= 0)
                {
                    MessageBox.Show("ID должен быть положительным целым числом.");
                    return;
                }

                var resultTable = _dbService.GetById(
                    selectedTable.TableName,
                    idColumn,
                    idValue
                );

                if (resultTable.Rows.Count == 0)
                {
                    MessageBox.Show(
                        "Запись с таким ID не найдена.",
                        "Результат поиска",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    return;
                }

                _currentTable = resultTable;
                MainDataGrid.ItemsSource = _currentTable.DefaultView;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при поиске записи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTable = GetSelectedTable();

                if (selectedTable == null)
                {
                    MessageBox.Show("Выберите таблицу.");
                    return;
                }

                var columns = _dbService.GetColumns(selectedTable.TableName);

                var values = new Dictionary<string, object?>();


                foreach (DataColumn column in _currentTable!.Columns)
                {
                    if (IsIdentityColumn(selectedTable.TableName, column.ColumnName))
                    {
                        continue;
                    }

                    string? input = Microsoft.VisualBasic.Interaction.InputBox(
                        $"Введите значение для поля \"{column.ColumnName}\":",
                        "Добавление записи",
                        ""
                    );

                    if (!ValidateInput(
                            selectedTable.TableName,
                            column.ColumnName,
                            input,
                            column.DataType,
                            column.AllowDBNull,
                            out string errorMessage))
                    {
                        MessageBox.Show($"Поле \"{column.ColumnName}\" не может быть пустым.");
                        return;
                    }

                    values[column.ColumnName] = ConvertInputValue(input, column.DataType);
                }

                _dbService.Insert(selectedTable.TableName, values);

                MessageBox.Show("Запись успешно добавлена.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка добавления записи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTable = GetSelectedTable();

                if (selectedTable == null)
                {
                    MessageBox.Show("Выберите таблицу.");
                    return;
                }

                if (MainDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите запись для обновления.");
                    return;
                }

                var rowView = MainDataGrid.SelectedItem as DataRowView;

                if (rowView == null)
                {
                    MessageBox.Show("Не удалось получить выбранную строку.");
                    return;
                }

                string idColumn = GetPrimaryKeyColumn(selectedTable.TableName);

                object idValue = rowView[idColumn];

                var values = new Dictionary<string, object?>();


                foreach (DataColumn column in _currentTable!.Columns)
                {
                    if (column.ColumnName == idColumn)
                    {
                        continue;
                    }

                    string oldValue = rowView[column.ColumnName]?.ToString() ?? "";

                    string? input = Microsoft.VisualBasic.Interaction.InputBox(
                        $"Введите новое значение для поля \"{column.ColumnName}\":",
                        "Обновление записи",
                        oldValue
                    );

                    if (!ValidateInput(
                            selectedTable.TableName,
                            column.ColumnName,
                            input,
                            column.DataType,
                            column.AllowDBNull,
                            out string errorMessage))
                    {
                        MessageBox.Show(
                            errorMessage,
                            "Ошибка валидации",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                        return;
                    }

                    values[column.ColumnName] = ConvertInputValue(input, column.DataType);
                }


                _dbService.Update(selectedTable.TableName, idColumn, idValue, values);

                MessageBox.Show("Запись успешно обновлена.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка обновления записи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedTable = GetSelectedTable();

                if (selectedTable == null)
                {
                    MessageBox.Show("Выберите таблицу.");
                    return;
                }

                if (MainDataGrid.SelectedItem == null)
                {
                    MessageBox.Show("Выберите запись для удаления.");
                    return;
                }

                var rowView = MainDataGrid.SelectedItem as DataRowView;

                if (rowView == null)
                {
                    MessageBox.Show("Не удалось получить выбранную строку.");
                    return;
                }

                var result = MessageBox.Show(
                    "Вы действительно хотите удалить выбранную запись?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                string idColumn = GetPrimaryKeyColumn(selectedTable.TableName);
                object idValue = rowView[idColumn];

                _dbService.Delete(selectedTable.TableName, idColumn, idValue);

                MessageBox.Show("Запись успешно удалена.");
                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка удаления записи:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private string GetPrimaryKeyColumn(string tableName)
        {
            return tableName switch
            {
                "факультет" => "id",
                "Обслуживающий персонал" => "id",
                "жилые помещения" => "id",
                "общаги" => "id",

                "общежития-персонал" => "id_персонала",

                "факультет-общежитие" => "id_общежития",

                _ => "id"
            };
        }

        private bool IsIdentityColumn(string tableName, string columnName)
        {
            if (tableName == "факультет" && columnName == "id")
                return true;

            if (tableName == "Обслуживающий персонал" && columnName == "id")
                return true;

            if (tableName == "жилые помещения" && columnName == "id")
                return true;

            if (tableName == "общаги" && columnName == "id")
                return true;

            if (tableName == "общежития-персонал" && columnName == "id_персонала")
                return true;

            if (tableName == "факультет-общежитие" && columnName == "id_общежития")
                return true;

            return false;
        }

        private object? ConvertInputValue(string? input, Type targetType)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return DBNull.Value;
            }

            if (targetType == typeof(int))
            {
                return int.Parse(input);
            }

            if (targetType == typeof(short))
            {
                return short.Parse(input);
            }

            if (targetType == typeof(long))
            {
                return long.Parse(input);
            }

            if (targetType == typeof(decimal))
            {
                return decimal.Parse(input);
            }

            if (targetType == typeof(double))
            {
                return double.Parse(input);
            }

            if (targetType == typeof(float))
            {
                return float.Parse(input);
            }

            if (targetType == typeof(DateTime))
            {
                return DateTime.Parse(input);
            }

            return input.Trim();
        }

        private bool ValidateInput(
                        string tableName,
                        string columnName,
                        string? input,
                        Type targetType,
                        bool allowNull,
                        out string errorMessage)
        {
            errorMessage = "";

            if (string.IsNullOrWhiteSpace(input))
            {
                if (!allowNull)
                {
                    errorMessage = $"Поле \"{columnName}\" не может быть пустым.";
                    return false;
                }

                return true;
            }


            if (columnName.StartsWith("id_", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(input, out int idValue))
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть целым числом.";
                    return false;
                }

                if (idValue <= 0)
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть больше нуля.";
                    return false;
                }
            }


            if (targetType == typeof(int))
            {
                if (!int.TryParse(input, out int value))
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть целым числом.";
                    return false;
                }

                if (columnName.Contains("Контакт", StringComparison.OrdinalIgnoreCase) && value <= 0)
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть положительным числом.";
                    return false;
                }
            }

            if (targetType == typeof(short))
            {
                if (!short.TryParse(input, out short value))
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть целым числом.";
                    return false;
                }

                if (value <= 0)
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть больше нуля.";
                    return false;
                }
            }

            if (targetType == typeof(decimal))
            {
                if (!decimal.TryParse(input, out decimal value))
                {
                    errorMessage = $"Поле \"{columnName}\" должно быть числом.";
                    return false;
                }

                if (value < 0)
                {
                    errorMessage = $"Поле \"{columnName}\" не может быть отрицательным.";
                    return false;
                }
            }

            if (targetType == typeof(string))
            {
                int maxLength = GetMaxLength(tableName, columnName);

                if (maxLength > 0 && input.Length > maxLength)
                {
                    errorMessage = $"Поле \"{columnName}\" не должно быть длиннее {maxLength} символов.";
                    return false;
                }
            }

            if (columnName.Contains("Контакт", StringComparison.OrdinalIgnoreCase) ||
                columnName.Contains("номер", StringComparison.OrdinalIgnoreCase))
            {
                string onlyDigits = input.Replace("+", "")
                                         .Replace("-", "")
                                         .Replace(" ", "")
                                         .Replace("(", "")
                                         .Replace(")", "");

                if (!onlyDigits.All(char.IsDigit))
                {
                    errorMessage = $"Поле \"{columnName}\" должно содержать только цифры и допустимые символы телефона.";
                    return false;
                }
            }

            return true;     
        }

        private void ApplyFilter(string filterText)
        {
            if (_currentTable == null)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(filterText))
            {
                _currentTable.DefaultView.RowFilter = "";
                return;
            }

            var filters = new List<string>();

            foreach (DataColumn column in _currentTable.Columns)
            {
                // только строки
                if (column.DataType == typeof(string))
                {
                    filters.Add($"[{column.ColumnName}] LIKE '%{filterText}%'");
                }
                else
                {
                    filters.Add($"CONVERT([{column.ColumnName}], 'System.String') LIKE '%{filterText}%'");
                }
            }

            string combinedFilter = string.Join(" OR ", filters);

            _currentTable.DefaultView.RowFilter = combinedFilter;
        }

        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string filterText = FilterTextBox.Text.Trim();
                ApplyFilter(filterText);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка фильтрации:\n{ex.Message}");
            }
        }

        private void ClearFilterButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentTable == null)
                return;

            FilterTextBox.Clear();
            _currentTable.DefaultView.RowFilter = "";
        }




    }
}