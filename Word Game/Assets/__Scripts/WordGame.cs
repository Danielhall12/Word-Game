using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public enum GameMode
{
    preGame,
    loading,
    makeLevel,
    levelPrep,
    inLevel
}
public class WordGame : MonoBehaviour
{
    static public WordGame S;

    [Header("Set in Inspector")]
    public GameObject         prefabLetter;
    public Rect               wordArea = new Rect( -24, 19, 48, 28 );
    public float              letterSize = 1.5f;
    public bool               showAllWyrds = true;
    
    public float bigLetterSize = 4f;
    public Color              bigColorDim = new Color( 0.8f, 0.8f, 0.8f );
    public Color              bigColorSelected = new Color( 1f, 0.9f, 0.7f );
    public Vector3            bigLetterCenter = new Vector3( 0, -16, 0 );
    
    [Header("Set Dynamically")] 
    public GameMode mode = GameMode.preGame;
    public WordLevel currLevel;
    public List<Wyrd> wyrds;
    
    public List<Letter>       bigLetters;
    public List<Letter>       bigLettersActive;
    
    private Transform letterAnchor, bigLetterAnchor;
    
    public void WordListParseComplete()
    {
        mode = GameMode.makeLevel;
        currLevel = MakeWordLevel();
    }

    public WordLevel MakeWordLevel(int levelNum=-1)
    {
        WordLevel level = new WordLevel();
        if (levelNum == -1)
        {
            level.longWordIndex = Random.Range(0, WordList.LONG_WORD_COUNT);
        }
        else
        {
            //added later
        }

        level.levelNum = levelNum;
        level.word = WordList.GET_LONG_WORD(level.longWordIndex);
        level.charDict = WordLevel.MakeCharDict(level.word);

        StartCoroutine(FindSubWordsCoroutine(level));

        return (level);
    }

    public IEnumerator FindSubWordsCoroutine(WordLevel level)
    {
        level.subWords = new List<string>();
        string str;

        List<string> words = WordList.GET_WORDS();

        for (int i = 0; i < WordList.WORD_COUNT; i++)
        {
            str = words[i];
            if (WordLevel.CheckWordInLevel(str, level))
            {
                level.subWords.Add(str);
            }

            if (i % WordList.NUM_TO_PARSE_BEFORE_YIELD == 0)
            {
                yield return null;
            }
        }

        level.subWords.Sort();
        level.subWords = SortWordsByLength(level.subWords).ToList();

        SubWordSearchComplete();
    }

    public static IEnumerable<string> SortWordsByLength(IEnumerable<string> ws)
    {
        ws = ws.OrderBy(s => s.Length);
        return ws;
    }

    public void SubWordSearchComplete()
    {
        mode = GameMode.levelPrep;
        Layout();
    }

    void Layout()
    {
        wyrds = new List<Wyrd>();

        GameObject go;
        Letter     lett;
        string     word;
        Vector3    pos;
        float      left = 0;
        float      columnWidth = 3;
        char       c;
        Color      col;
        Wyrd       wyrd;
        
        int numRows = Mathf.RoundToInt(wordArea.height/letterSize);
 
        // Make a Wyrd of each level.subWord
        for (int i=0; i<currLevel.subWords.Count; i++) {
            wyrd = new Wyrd();
            word = currLevel.subWords[i];
 
            // if the word is longer than columnWidth, expand it
            columnWidth = Mathf.Max( columnWidth, word.Length );
 
            // Instantiate a PrefabLetter for each letter of the word
            for (int j=0; j<word.Length; j++) {
                c = word[j]; // Grab the jth char of the word
                go = Instantiate<GameObject>( prefabLetter );
                go.transform.SetParent( letterAnchor );
                lett = go.GetComponent<Letter>();
                lett.c = c; // Set the c of the Letter
 

                // Position the Letter
                pos = new Vector3(wordArea.x+left+j*letterSize, wordArea.y, 0);
 

                // The % here makes multiple columns line up
                pos.y -= (i%numRows)*letterSize;
 

                lett.pos = pos;    // You'll add more code around this line later
 

                go.transform.localScale = Vector3.one*letterSize;
 

                wyrd.Add(lett);
 
            }
 
            if (showAllWyrds) wyrd.visible = true;
 
            wyrds.Add(wyrd);
 
            // If we've gotten to the numRows(th) row, start a new column
            if ( i%numRows == numRows-1 ) {
                left += ( columnWidth + 0.5f ) * letterSize;
            }
        }
        
        bigLetters = new List<Letter>();
        bigLettersActive = new List<Letter>();

        for (int i = 0; i < currLevel.word.Length; i++)
        {
            c = currLevel.word[i];
            go = Instantiate<GameObject>( prefabLetter );
            go.transform.SetParent( bigLetterAnchor );
            lett = go.GetComponent<Letter>();
            lett.c = c;
            go.transform.localScale = Vector3.one*bigLetterSize;
 
            // Set the initial position of the big Letters below screen
            pos = new Vector3( 0, -100, 0 );
            lett.pos = pos;        // You'll add more code around this line later
 
            col = bigColorDim;
            lett.color = col;
            lett.visible = true; // This is always true for big letters
            lett.big = true;
            bigLetters.Add(lett);
        }
        bigLetters = ShuffleLetters(bigLetters);
        // Arrange them on screen
        ArrangeBigLetters();
 
        // Set the mode to be in-game
        mode = GameMode.inLevel;
        
    }
    
    List<Letter> ShuffleLetters(List<Letter> letts) {
        List<Letter> newL = new List<Letter>();
        int ndx;
        while(letts.Count > 0) {
            ndx = Random.Range(0,letts.Count);
            newL.Add(letts[ndx]);
            letts.RemoveAt(ndx);
        }
        return(newL);
    }
 
    // This method arranges the big Letters on screen
    void ArrangeBigLetters() {
        // The halfWidth allows the big Letters to be centered
        float halfWidth = ( (float) bigLetters.Count )/2f - 0.5f;
        Vector3 pos;
        for (int i=0; i<bigLetters.Count; i++) {
            pos = bigLetterCenter;
            pos.x += (i-halfWidth)*bigLetterSize;
            bigLetters[i].pos = pos;
        }
        // bigLettersActive
        halfWidth = ( (float) bigLettersActive.Count )/2f - 0.5f;
        for (int i=0; i<bigLettersActive.Count; i++) {
            pos = bigLetterCenter;
            pos.x += (i-halfWidth)*bigLetterSize;
            pos.y += bigLetterSize*1.25f;
            bigLettersActive[i].pos = pos;
        }
    }
    
    // Start is called before the first frame update
    void Start()
    {
        mode = GameMode.loading;
        WordList.INIT();
    }

    // Update is called once per frame
    void Awake()
    {
        S = this;
        letterAnchor= new GameObject("LetterAnchor").transform;
        bigLetterAnchor=new GameObject("BigLetterAnchor").transform;
    }

   
}
