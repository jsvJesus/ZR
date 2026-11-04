package ui.screens
{
   import com.dvalimona.components.*;
   import communication.*;
   import events.*;
   import flash.events.*;
   import lang.*;
   import logging.*;
   import ui.Screen;
   import ui.components.*;
   
   public class CharScreen extends Screen
   {
      protected var vBox:VBox;
      
      protected var linesBox:VBox;
      
      protected var headerPanel:HeaderPanel;
      
      protected var backButton:MenuButton;
      
      protected var quad:Quad;
      
      protected var serviceButton:PushButton;
      
      private var buttons:Array;
      
      public function CharScreen(param1:String, param2:uint = 0)
      {
         Character.core.addEventListener(Character.UPDATED,this.onCharactersUpdated);
         Character.core.addEventListener(Character.CHANGE,this.onCharacterChange);
         super(param1,param2,true);
      }
      
      override protected function unfreeze(... rest) : void
      {
         this.label = BreadCrumbs.DIV + Locale.getById("extendedGUI.CharWindow.yourChars");
         super.unfreeze();
         Logger.LogToChannel(Logger.DEBUG,"CharScreen.unfreeze");
         this.makeListView();
      }
      
      override protected function init(... rest) : void
      {
         this.vBox = new VBox();
         this.vBox.x = 50;
         this.vBox.y = 100;
         this.vBox.alignment = VBox.LEFT;
         this.vBox.spacing = 10;
         this.vBox.debug = false;
         super.addChild(this.vBox);
         this.serviceButton = new MenuButton();
         this.serviceButton.$ = "extendedGUI.CharWindow.createChar";
         this.serviceButton.underline = false;
         this.serviceButton.size = 18;
         this.serviceButton.align = Label.RIGHT;
         this.serviceButton.debug = false;
         this.serviceButton.autoWidth = true;
         this.serviceButton.enabled = false;
         this.serviceButton.paddingRight = 10;
         this.serviceButton.addEventListener(MouseEvent.CLICK,this.createCharClickHandler);
         this.linesBox = new VBox();
         this.linesBox.alignment = VBox.LEFT;
         this.linesBox.spacing = 1;
         this.linesBox.debug = true;
         this.linesBox.width = widths[0];
         this.vBox.addChild(this.linesBox);
         this.quad = new Quad();
         this.quad.width = widths[0];
         this.quad.height = 10;
         this.backButton = new MenuButton();
         this.backButton.$ = "extendedGUI.CharWindow.backButton";
         this.backButton.height = 30;
         this.backButton.paddingLeft = 10;
         this.backButton.addEventListener(MouseEvent.CLICK,this.onBackButtonHandler);
      }
      
      protected function createCharClickHandler(param1:MouseEvent) : void
      {
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.NEW_CHAR_SCREEN));
      }
      
      private function makeListView() : void
      {
         this.clearLinesBox();
         this.vBox.draw();
         this.updateContent();
      }
      
      protected function onCharacterChange(param1:Event) : void
      {
         this.updateContent();
      }
      
      protected function onCharactersUpdated(param1:Event) : void
      {
         this.updateContent();
      }
      
      private function onBackButtonHandler(param1:Event = null) : void
      {
         trace("onQuitHandler");
         this.goBack();
      }
      
      override public function goBack() : void
      {
         this.dispatchEvent(new ScreenEvent(ScreenEvent.GO_SCREEN,MainMenuGUI.ROOT_SCREEN));
         BreadCrumbs.Remove(this.breadCrumb);
      }
      
      override protected function resize(... rest) : void
      {
      }
      
      protected function updateContent() : void
      {
         var charButton:CharButton = null;
         var char:Character = null;
         var i:uint = 0;
         var newCharacter:MenuButton = null;
         Logger.LogToChannel(Logger.DEBUG,"CharScreen.updateContent, Character.list.length:",Character.list.length);
         if(!inited)
         {
            return;
         }
         try
         {
            this.serviceButton.enabled = Character.AllowNewCharCreating;
            appears = [];
            this.clearLinesBox();
            i = 0;
            while(i < Character.MAX_COUNT)
            {
               char = Character.list[i] as Character;
               if(char != null)
               {
                  charButton = new CharButton(this.vBox);
                  charButton.character = char;
                  charButton.selected = char.id == Character.currentId;
                  charButton.groupName = "char_group";
                  charButton.height = 42;
                  charButton.paddingTop = -5;
                  charButton.paddingBottom = -5;
                  charButton.width = widths[0];
                  charButton.draw();
               }
               else
               {
                  newCharacter = new MenuButton(this.vBox);
                  newCharacter.$ = "extendedGUI.CharWindow.createChar";
                  newCharacter.height = 30;
                  newCharacter.paddingLeft = 10;
                  newCharacter.addEventListener(MouseEvent.CLICK,this.createCharClickHandler);
                  appears.push(newCharacter);
               }
               i++;
            }
            this.vBox.addChild(this.quad);
            this.vBox.addChild(this.backButton);
            appears.push(this.quad);
            appears.push(this.backButton);
            appears.push(this.vBox);
            this.linesBox.draw();
            this.vBox.draw();
         }
         catch(error:Error)
         {
            Logger.LogToChannel(Logger.ERROR,"CharScreen.updateContent error:",error);
         }
      }
      
      private function clearLinesBox() : void
      {
         while(this.vBox.numChildren > 0)
         {
            this.vBox.removeChildAt(0);
         }
      }
   }
}

