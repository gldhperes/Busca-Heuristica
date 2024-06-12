using System.Collections.Generic;
using UnityEngine;

public class BuscaHeuristica : MonoBehaviour
{

    [Header("Init Config")]
    public Mozaic inicial = null;
    public Mozaic objetivo = null;
    public GameObject mozaic_Prefeb;
    public GameObject solution_panel;
    public GameObject solutionMozaic;
    public TempoController temp = null;

    [Header("Vars")]
    [SerializeField] private List<Mozaic> abertos = new();
    [SerializeField] private List<Mozaic> fechados = new();
    [SerializeField] private Mozaic X;
    [Space(20)]

    [Header("Conditionals")]
    public bool solutionFounded = false;
    [SerializeField] private bool fail = false;
    [SerializeField] private bool play_busca = false;

    void Start()
    {
        // Seta os estados iniciais dos mozaicos
        inicial.Set_State();
        objetivo.Set_State();

        // Seta o painel de solução como falso
        solution_panel.SetActive(false);

        // Altera o mozaico de solução para ser uma copia do inicial
        solutionMozaic.GetComponent<Mozaic>().Copy_State_From_Initial_Mozaic(inicial);

        // Adiciono o mozaico inicial em abertos
        abertos.Add(inicial);
    }



    void Update()
    {
        if (!solutionFounded && !fail && play_busca)
        {
            BuscarHeuristica();
        }
    }

    private void BuscarHeuristica()
    {
        // Enquanto aberto diferente de vazio
        if (abertos.Count > 0)
        {
            Mozaic aux = abertos[0];

            // Remove o primeiro elemento de abertos
            abertos.RemoveAt(0);

            // Seta X como o 1º elemento de abertos (que ja foi excuido)
            X = aux;


            // Checa se o estado de X é igual ao do objetivo
            // Se sim, coloca solutionFounded como true, para sair do laço de Update
            // Pausa o contador
            // E seta variaveis do painel de solução
            if (Check_Equals_States(X, objetivo))
            {

                solutionFounded = true;

                temp.PausarContador();

                Set_Solution_Mozaics();

            }
            else
            {
                // Gera filhos de X
                // Esses filhos são gerados pela possibilidade da jogada

                Debug.Log(X.mozaicName + " gerando filhos...");
                X.Create_Childrens();

                // Para cada filho de X
                Foreach_Xs_Children();
            }

            // Adiciona X a fechados
            fechados.Add(X);

            // Calcula por valor heuristico
            Sort_By_Heuristic_Value();

        }
    }

    #region HEURISTIC_REGION
    private bool Check_Equals_States(Mozaic a, Mozaic b)
    {
        bool equal = false;


        if (a.Get_State_String() == b.Get_State_String())
        {
            Debug.Log(b.mozaicName + ": " + b.Get_State_String());
            Debug.Log(a.mozaicName + ": " + a.Get_State_String());
            Debug.Log("Mozaicos iguais!");
            equal = true;
        }

        return equal;
    }

    private class Search_Result
    {
        public Mozaic mozaic = null;
        public bool founded = false;

        public Search_Result(Mozaic mozaic, bool founded)
        {
            this.mozaic = mozaic;
            this.founded = founded;
        }
        public Search_Result() { }
    }

    private void Foreach_Xs_Children()
    {
        List<Mozaic> to_Add_Abertos = new();
        List<Mozaic> to_Remove_On_X = new();

        foreach (Mozaic children in X.Get_Childrens())
        {
            Debug.Log("Filho de X: " + children.mozaicName + " state: " + children.Get_State_String());

            // Faz buscas nas listas pelo estado do Mozaico
            Search_Result opened_founded = Search_On_List(abertos, children);
            Search_Result closed_founded = Search_On_List(fechados, children);


            // Se algum filho gerado NAO estiver em abertos e fechados
            // Logo, so gera valor heuristico para os que nao estao nessas listas
            if ((opened_founded.founded == false) && (closed_founded.founded == false))
            {
                Debug.Log("Filho nao existe, acrecentando em abertos");

                // Atribuir um Valor Heuristico ao Filho
                children.Set_Heuristic_Value(objetivo);

                // Acrescenta filhos à abertos
                to_Add_Abertos.Add(children);
            }
            else if (opened_founded.founded)
            {
                Openeds_Founded(children, opened_founded.mozaic, to_Add_Abertos, to_Remove_On_X);
            }
            else if (closed_founded.founded)
            {
                Closeds_Founded(children, closed_founded.mozaic, to_Add_Abertos, to_Remove_On_X);
            }
            // else
            // {
            //     Debug.Log("Filho ja existe, Destruindo");
            //     Destroy(children.gameObject);
            // }

        }

        // Adiciona os itens de to_Add_Abertos
        // Essa lista foi criada com o proposito de nao repetir os itens que esta em abertos
        foreach (Mozaic item in to_Add_Abertos)
        {
            abertos.Add(item);
        }

        foreach (Mozaic item in to_Remove_On_X)
        {
            X.Get_Childrens().Remove(item);
        }
    }

    private void Openeds_Founded(Mozaic children, Mozaic mozaic_on_list, List<Mozaic> to_Add_Abertos, List<Mozaic> to_Remove_On_X)
    {

        Debug.Log("FILHO JA EXISTENTE EM ABERTOS");

        if (Children_Founded(children, mozaic_on_list, to_Remove_On_X))
        {
            Debug.Log("Caminho mais curto: " + children.mozaicName);

            // Remove Opened_mozaic 
            abertos.Remove(mozaic_on_list);
            Debug.Log("Removendo de abertos: " + mozaic_on_list.mozaicName);

            //Adiciona o Child
            to_Add_Abertos.Add(children);
            Debug.Log("Adicionando em abertos: " + children.mozaicName);

        }

        Debug.Log("==============");

    }

    private void Closeds_Founded(Mozaic children, Mozaic mozaic_on_list, List<Mozaic> to_Add_Abertos, List<Mozaic> to_Remove_On_X)
    {
        Debug.Log("FILHO JA EXISTENTE EM FECHADOS");

        if (Children_Founded(children, mozaic_on_list, to_Remove_On_X))
        {
            Debug.Log("Filho com caminho mais curto");

            fechados.Remove(mozaic_on_list);
            Debug.Log("Removendo de fechados: " + mozaic_on_list.mozaicName);

            abertos.Add(children);
            Debug.Log("Adicionando em abertos: " + children.mozaicName);
        }

        Debug.Log("==============");
    }

    private bool Children_Founded(Mozaic children, Mozaic mozaic_on_list, List<Mozaic> to_Remove_On_X)
    {
        // Compara o Filho e mozaic_on_list com o menor valor de jogadas
        if (children.Get_Moves_Taken.Length < mozaic_on_list.Get_Moves_Taken.Length)
        {
            return true;
        }
        else
        {
            // Nesta parte do codigo ele nao faz nada
            // Pois o caminho nao é o mais curto
            // Isso acaba resultando em anulação do Mozaico / caminho
            Debug.Log("Destruindo caminho mais longo: " + children);

            to_Remove_On_X.Add(children);

            Destroy(children.gameObject);
            return false;
        }
    }


    private Search_Result Search_On_List(List<Mozaic> list, Mozaic mozaic)
    {

        Search_Result result = new()
        {
            mozaic = null,
            founded = false
        };

        if (list.Count > 0)
        {
            foreach (Mozaic mozaic_on_list in list)
            {
                if (Check_Equals_States(mozaic_on_list, mozaic))
                {
                    result.mozaic = mozaic_on_list;
                    result.founded = true;
                    break;
                }
            }
        }

        Debug.Log("Achado? " + result.founded);
        return result;
    }

    private void Sort_By_Heuristic_Value()
    {
        Debug.Log("Reorganizando Abertos");
        abertos.Sort((a, b) => a.Get_Heuristic_Value().CompareTo(b.Get_Heuristic_Value()));
    }
    #endregion



    private void Set_Solution_Mozaics()
    {
        solution_panel.SetActive(true);

        // Cria um 'clone' da lista de fechados e passa para o painel de solução
        List<Mozaic> temporary = new(fechados);

        Solution_Founded_Panel_Behaviour sfpb = solution_panel.GetComponent<Solution_Founded_Panel_Behaviour>();
        sfpb.fechados = temporary;
        sfpb.fechados.Add(X);
        sfpb.X = this.X;

        sfpb.Set_Objective_Mozaic(this.objetivo);
        sfpb.Set_Temp(temp.ObterTempoDecorrido());

        sfpb.Start_Solution_Animation();

    }

    #region PLAY_REGION
    public void Play_Busca()
    {
        play_busca = true;

        temp.IniciarContador();
    }

    public void Play_Again()
    {
        temp.ReiniciarContador();

        inicial.Reset_Generated_Childrens();

        abertos.Clear();
        abertos.Add(inicial);
        fechados.Clear();
        X = null;

        GameObject generated_Childrens_Storage = GameObject.Find("Generated_Childrens_Storage");

        for (int i = 0; i < generated_Childrens_Storage.transform.childCount; i++)
        {
            Destroy(generated_Childrens_Storage.transform.GetChild(i).gameObject);
        }


        solutionFounded = false;
        fail = false;
        play_busca = false;

    }
    #endregion
}