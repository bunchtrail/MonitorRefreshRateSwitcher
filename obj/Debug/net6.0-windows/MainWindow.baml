   M S B A M L   `   `   `  ����U  QMonitorRefreshRateSwitcher, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullG(clr-namespace:MonitorRefreshRateSwitcherMonitorRefreshRateSwitcher  5   	   +    %MonitorRefreshRateSwitcher.MainWindow   5   	   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   :local(clr-namespace:MonitorRefreshRateSwitcher     5   	     � Title$U  OПереключатель частоты обновления монитора��5   	   $	��200��6;   $	��400��6H    � WindowStartupLocation$ CenterScreen=�5	   	   .��5
      � $��10q�6    � RowDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $
��Auto �6   �� 5      $��* �6   .+�5   
   �� $��0��6   $;��5Выберите частоту обновления:��6!   $��0,0,0,5q�6E   �� 5   
   -   5         ��Name$ RefreshRatesComboBox��5       $��1��6   $��0,0,0,10q�5      5      �� 5   
   -   5         ��Name$ StatusTextBlock��5   !   $��2��6   $��0,10,0,0q�5      $
��Wrap=�5      5      5      